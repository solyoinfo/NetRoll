using Microsoft.AspNetCore.Identity;

namespace NetRoll.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
    // Előfizetési / csomag azonosító (pl. FREE, PRO)
    public string? PlanName { get; set; }
    }

}
