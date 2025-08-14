using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetRoll.Components;
using NetRoll.Components.Account;
using NetRoll.Data;
using NetRoll.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Reduce EF Core command log noise (only warnings and above)
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN"; // header-based option for fetch/XHR if needed
});
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Normalize cookie names/paths so logout can reliably clear them
builder.Services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
{
    options.Cookie.Name = ".NetRoll.Identity.Application"; // distinct, predictable
    options.Cookie.Path = "/";
});
builder.Services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ExternalScheme, options =>
{
    options.Cookie.Name = ".NetRoll.Identity.External";
    options.Cookie.Path = "/";
});

// Authorization services are required when protecting endpoints/components
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Email sender (SMTP)
builder.Services.AddScoped<IEmailSender, NetRoll.Services.SmtpEmailSender>();
// Also register concrete for components that inject SmtpEmailSender directly
builder.Services.AddScoped<NetRoll.Services.SmtpEmailSender>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        // Allow spaces and Hungarian accented letters in usernames
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ áéíóöőúüűÁÉÍÓÖŐÚÜŰ";
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Replace no-op email sender with real SMTP-based sender
// builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>(); // replaced by SMTP sender
builder.Services.AddSingleton<MenuService>();
builder.Services.AddScoped<NetRoll.Services.ImageStorageService>();
builder.Services.AddSingleton<NetRoll.Services.PlanService>();
builder.Services.AddSingleton<NetRoll.Services.IHtmlSanitizerService, NetRoll.Services.HtmlSanitizerService>();
builder.Services.AddHttpClient();

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("hu"), new CultureInfo("en") };
    options.DefaultRequestCulture = new RequestCulture("hu");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    // Prefer cookie culture provider
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});


// Configure HTTPS redirection options (only used in production below)
builder.Services.AddHttpsRedirection(o =>
{
    // Default dev HTTPS port from launchSettings; adjust if different
    o.HttpsPort = 7237;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    // Development alatt nem erőltetünk HTTPS redirectet, ha csak HTTP profil fut → elkerüljük a warningot.
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}


// Ensure authentication/authorization middleware are in the pipeline
app.UseAuthentication();
app.UseAuthorization();

// Request localization
var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Endpoint to set culture via cookie and redirect back
app.MapGet("/set-culture/{culture}", (string culture, HttpContext ctx) =>
{
    var supported = new[] { "hu", "en" };
    if (!supported.Contains(culture)) culture = "hu";
    var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
    ctx.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, cookieValue, new CookieOptions
    {
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        Path = "/"
    });
    var referer = ctx.Request.Headers["Referer"].ToString();
    return Results.Redirect(string.IsNullOrWhiteSpace(referer) ? "/" : referer);
});

// Seed alap szerepek (Admin, Editor, Viewer)
using (var scope = app.Services.CreateScope())
{
    // Apply pending EF Core migrations at startup
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Database migration failed: {ex.Message}");
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Editor", "Viewer" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

app.Run();
