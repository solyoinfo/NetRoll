using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using NetRoll.Data;

namespace NetRoll.Controllers
{
    [Authorize(Roles="Admin")]
    [ApiController]
    [Route("Account")] // matches form action
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;
    private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender _emailSender;

    public AccountController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext db, Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender)
        {
            _userManager = userManager; _roleManager = roleManager; _db = db; _emailSender = emailSender;
        }

    [HttpPost("SetRolesAndPlan")]
    [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRolesAndPlan([FromForm] string userId, [FromForm] string[]? roles, [FromForm] string? planName, [FromForm] string? returnUrl)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            var currentRoles = await _userManager.GetRolesAsync(user);
            // Simple role sync: remove all then add selected
            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            if (roles != null && roles.Length > 0)
            {
                foreach (var r in roles.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!await _roleManager.RoleExistsAsync(r))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(r));
                    }
                }
                await _userManager.AddToRolesAsync(user, roles);
            }
            user.PlanName = string.IsNullOrWhiteSpace(planName) ? null : planName.Trim();
            await _userManager.UpdateAsync(user);
            // Auto-resolve any pending plan change requests for this user
            var pending = _db.PlanChangeRequests.Where(r => r.UserId == user.Id && r.Status == NetRoll.Models.PlanChangeStatus.Pending).ToList();
            if (pending.Count > 0)
            {
                var admin = await _userManager.GetUserAsync(User);
                foreach (var req in pending)
                {
                    if (!string.IsNullOrWhiteSpace(user.PlanName) && string.Equals(req.RequestedPlan, user.PlanName, StringComparison.OrdinalIgnoreCase))
                        req.Status = NetRoll.Models.PlanChangeStatus.Approved;
                    else
                        req.Status = NetRoll.Models.PlanChangeStatus.Rejected;
                    req.ProcessedUtc = DateTime.UtcNow;
                    req.ProcessedByUserId = admin?.Id;
                }
                await _db.SaveChangesAsync();
            }
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl + (returnUrl.Contains('?') ? "&" : "?") + "saved=1");
            return Redirect("/users?saved=1");
        }

        [AllowAnonymous]
        [HttpPost("RequestPlanChange")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPlanChange([FromForm] string requestedPlan, [FromForm] string? comment, [FromForm] string? returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (string.IsNullOrWhiteSpace(requestedPlan)) return BadRequest();
            // Rate limiting: only one pending, and at least 5 minutes after last attempt
            var now = DateTime.UtcNow;
            var existingPending = _db.PlanChangeRequests.Any(r => r.UserId == user.Id && r.Status == NetRoll.Models.PlanChangeStatus.Pending);
            if (existingPending)
            {
                // Already has a pending request
                HttpContext.Items["PlanChangeInfo"] = "AlreadyPending";
                // silent redirect with flag? reuse planRequested but add another indicator
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl + (returnUrl.Contains('?')?"&":"?") + "planAlreadyPending=1");
                return Redirect("/?planAlreadyPending=1");
            }
            var recent = _db.PlanChangeRequests.Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedUtc).FirstOrDefault();
            if (recent != null && (now - recent.CreatedUtc) < TimeSpan.FromMinutes(5))
            {
                HttpContext.Items["PlanChangeInfo"] = "RateLimited";
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl + (returnUrl.Contains('?')?"&":"?") + "planRateLimited=1");
                return Redirect("/?planRateLimited=1");
            }
            var req = new NetRoll.Models.PlanChangeRequest
            {
                UserId = user.Id,
                CurrentPlan = user.PlanName,
                RequestedPlan = requestedPlan.Trim(),
                Comment = comment,
                CreatedUtc = now
            };
            _db.PlanChangeRequests.Add(req);
            await _db.SaveChangesAsync();
            // Notify admins
            await NotifyAdminsPlanRequestedAsync(user, req);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl + (returnUrl.Contains('?')?"&":"?") + "planRequested=1");
            return Redirect("/?planRequested=1");
        }

        [Authorize(Roles="Admin")]
        [HttpPost("ProcessPlanChange")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPlanChange([FromForm] int id, [FromForm] string action, [FromForm] string? returnUrl)
        {
            var req = await _db.PlanChangeRequests.FindAsync(id);
            if (req == null) return NotFound();
            if (req.Status != NetRoll.Models.PlanChangeStatus.Pending) return BadRequest();
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return Unauthorized();
            if (string.Equals(action, "approve", StringComparison.OrdinalIgnoreCase))
            {
                req.Status = NetRoll.Models.PlanChangeStatus.Approved;
                req.ProcessedUtc = DateTime.UtcNow;
                req.ProcessedByUserId = admin.Id;
                // set user plan
                var user = await _userManager.FindByIdAsync(req.UserId);
                if (user != null)
                {
                    user.PlanName = req.RequestedPlan;
                    await _userManager.UpdateAsync(user);
                }
            }
            else if (string.Equals(action, "reject", StringComparison.OrdinalIgnoreCase))
            {
                req.Status = NetRoll.Models.PlanChangeStatus.Rejected;
                req.ProcessedUtc = DateTime.UtcNow;
                req.ProcessedByUserId = admin.Id;
            }
            else return BadRequest();
            await _db.SaveChangesAsync();
            // Notify admins on processing result
            var requestingUser = await _userManager.FindByIdAsync(req.UserId) ?? new ApplicationUser{Id=req.UserId, Email=""};
            await NotifyAdminsPlanProcessedAsync(requestingUser, req, admin);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return Redirect("/admin/plan-requests");
        }

    private async Task NotifyAdminsPlanRequestedAsync(ApplicationUser requester, NetRoll.Models.PlanChangeRequest req)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                if (!string.IsNullOrWhiteSpace(admin.Email))
                {
                    var subject = $"Plan change requested: {requester.UserName ?? requester.Email}";
                    var body = $"User: {System.Net.WebUtility.HtmlEncode(requester.UserName ?? requester.Email)}<br/>Current: {System.Net.WebUtility.HtmlEncode(req.CurrentPlan)}<br/>Requested: {System.Net.WebUtility.HtmlEncode(req.RequestedPlan)}<br/>Comment: {System.Net.WebUtility.HtmlEncode(req.Comment)}";
            HttpContext?.RequestServices.GetService<ILogger<AccountController>>()?.LogInformation("Sending plan request email to {Email}", admin.Email);
                    await _emailSender.SendEmailAsync(admin.Email!, subject, body);
                }
            }
        }

    private async Task NotifyAdminsPlanProcessedAsync(ApplicationUser requester, NetRoll.Models.PlanChangeRequest req, ApplicationUser adminUser)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                if (!string.IsNullOrWhiteSpace(admin.Email))
                {
                    var subject = $"Plan change {req.Status}: {requester.UserName ?? requester.Email}";
                    var body = $"User: {System.Net.WebUtility.HtmlEncode(requester.UserName ?? requester.Email)}<br/>Status: {req.Status}<br/>Requested: {System.Net.WebUtility.HtmlEncode(req.RequestedPlan)}<br/>Processed by: {System.Net.WebUtility.HtmlEncode(adminUser.UserName ?? adminUser.Email)}";
            HttpContext?.RequestServices.GetService<ILogger<AccountController>>()?.LogInformation("Sending plan processed email to {Email}", admin.Email);
                    await _emailSender.SendEmailAsync(admin.Email!, subject, body);
                }
            }
        }
    }
}
