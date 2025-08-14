using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using NetRoll.Components.Account.Pages;
using NetRoll.Components.Account.Pages.Manage;
using NetRoll.Data;

namespace Microsoft.AspNetCore.Routing
{
    internal static class IdentityComponentsEndpointRouteBuilderExtensions
    {
        // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
        public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var accountGroup = endpoints.MapGroup("/Account");

            accountGroup.MapPost("/PerformExternalLogin", (
                HttpContext context,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromForm] string provider,
                [FromForm] string returnUrl) =>
            {
                IEnumerable<KeyValuePair<string, StringValues>> query = [
                    new("ReturnUrl", returnUrl),
                    new("Action", ExternalLogin.LoginCallbackAction)];

                var redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/ExternalLogin",
                    QueryString.Create(query));

                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return TypedResults.Challenge(properties, [provider]);
            });

            accountGroup.MapPost("/Logout", async (
                HttpContext context,
                ClaimsPrincipal user,
                [FromServices] SignInManager<ApplicationUser> signInManager) =>
            {
                // Clear any external sign-in cookie and the primary application cookie
                await context.SignOutAsync(IdentityConstants.ExternalScheme);
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                await signInManager.SignOutAsync();
                // Additionally, ensure deletion by removing cookies directly (both scheme keys and explicit names)
                var cookieOptions = new CookieOptions { Path = "/" };
                foreach (var name in new[]
                {
                    IdentityConstants.ApplicationScheme,
                    IdentityConstants.ExternalScheme,
                    ".NetRoll.Identity.Application",
                    ".NetRoll.Identity.External",
                    ".AspNetCore.Identity.Application",
                    ".AspNetCore.Identity.External"
                })
                {
                    context.Response.Cookies.Delete(name, cookieOptions);
                }
                return TypedResults.LocalRedirect("/Account/Login");
            });

            // Optional: Also support GET to simplify simple anchor-based logout links
            accountGroup.MapGet("/Logout", async (
                HttpContext context,
                ClaimsPrincipal user,
                [FromServices] SignInManager<ApplicationUser> signInManager) =>
            {
                await context.SignOutAsync(IdentityConstants.ExternalScheme);
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                await signInManager.SignOutAsync();
                var cookieOptions2 = new CookieOptions { Path = "/" };
                foreach (var name in new[]
                {
                    IdentityConstants.ApplicationScheme,
                    IdentityConstants.ExternalScheme,
                    ".NetRoll.Identity.Application",
                    ".NetRoll.Identity.External",
                    ".AspNetCore.Identity.Application",
                    ".AspNetCore.Identity.External"
                })
                {
                    context.Response.Cookies.Delete(name, cookieOptions2);
                }
                return TypedResults.LocalRedirect("/Account/Login");
            });

            var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

            manageGroup.MapPost("/LinkExternalLogin", async (
                HttpContext context,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromForm] string provider) =>
            {
                // Clear the existing external cookie to ensure a clean login process
                await context.SignOutAsync(IdentityConstants.ExternalScheme);

                var redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/Manage/ExternalLogins",
                    QueryString.Create("Action", ExternalLogins.LinkLoginCallbackAction));

                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User));
                return TypedResults.Challenge(properties, [provider]);
            });

            var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

            manageGroup.MapPost("/DownloadPersonalData", async (
                HttpContext context,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                {
                    return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
                }

                var userId = await userManager.GetUserIdAsync(user);
                downloadLogger.LogInformation("User with ID '{UserId}' asked for their personal data.", userId);

                // Only include personal data for download
                var personalData = new Dictionary<string, string>();
                var personalDataProps = typeof(ApplicationUser).GetProperties().Where(
                    prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
                foreach (var p in personalDataProps)
                {
                    personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
                }

                var logins = await userManager.GetLoginsAsync(user);
                foreach (var l in logins)
                {
                    personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
                }

                personalData.Add("Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!);
                var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

                context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
                return TypedResults.File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
            });

            // Admin-only: szerep kapcsolása egy felhasználón (hozzáadás/elvétel)
            accountGroup.MapPost("/ToggleRole", async (
                HttpContext context,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] RoleManager<IdentityRole> roleManager,
                [FromForm] string userId,
                [FromForm] string role) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return Results.NotFound();
                }

                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }

                if (await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.RemoveFromRoleAsync(user, role);
                }
                else
                {
                    await userManager.AddToRoleAsync(user, role);
                }

                var referer = context.Request.Headers["Referer"].ToString();
                var target = string.IsNullOrWhiteSpace(referer) ? "/users?saved=1" : (referer.Contains("?") ? referer + "&saved=1" : referer + "?saved=1");
                return Results.Redirect(target);
            }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

            // Admin-only: szerepek beállítása (több szerep egyszerre)
            accountGroup.MapPost("/SetRoles", async (
                HttpContext context,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] RoleManager<IdentityRole> roleManager,
                    [FromForm] string userId,
                    [FromForm] string? returnUrl) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return Results.NotFound();
                }

                var allowed = new[] { "Admin", "Editor", "Viewer" };
                // ensure roles exist
                foreach (var r in allowed)
                {
                    if (!await roleManager.RoleExistsAsync(r))
                        await roleManager.CreateAsync(new IdentityRole(r));
                }

                var current = await userManager.GetRolesAsync(user);
                var formRoles = context.Request.HasFormContentType ? context.Request.Form["roles"].ToArray() : Array.Empty<string>();
                var desired = formRoles?.Where(r => allowed.Contains(r, StringComparer.OrdinalIgnoreCase))
                                     .Select(r => allowed.First(a => a.Equals(r, StringComparison.OrdinalIgnoreCase)))
                                     .Distinct()
                                     .ToList() ?? new List<string>();

                var toRemove = current.Where(r => allowed.Contains(r) && !desired.Contains(r)).ToList();
                var toAdd = desired.Where(r => !current.Contains(r)).ToList();

                if (toRemove.Count > 0)
                    await userManager.RemoveFromRolesAsync(user, toRemove);
                if (toAdd.Count > 0)
                    await userManager.AddToRolesAsync(user, toAdd);

                    string? dest = returnUrl;
                    if (string.IsNullOrWhiteSpace(dest))
                    {
                        dest = context.Request.Headers["Referer"].ToString();
                    }
                    if (string.IsNullOrWhiteSpace(dest)) dest = "/users";
                    var target = dest.Contains("?") ? dest + "&saved=true" : dest + "?saved=true";
                    return Results.Redirect(target);
            }).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

            return accountGroup;
        }
    }
}
