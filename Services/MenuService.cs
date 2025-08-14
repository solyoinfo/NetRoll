using Microsoft.Extensions.Localization;
using NetRoll.Models;

namespace NetRoll.Services
{
    public class MenuService
    {
        private readonly IStringLocalizer<MenuService> L;

        public MenuService(IStringLocalizer<MenuService> localizer)
        {
            L = localizer;
        }

        public List<MenuItem> GetMenuItems()
        {
            return new List<MenuItem>
            {
                new MenuItem(L["Dashboard"], "oi oi-home", "/"),
                new MenuItem(L["Users"], "oi oi-people", "/users"),
                // Főmenü: Törzsadatok (lokalizálva)
                new MenuItem(L["MasterData"], "oi oi-layers", "/catalog", new List<MenuItem>{
                    new MenuItem(L["Languages"], "oi oi-globe", "/catalog/languages"),
                    new MenuItem(L["Categories"], "oi oi-list", "/catalog/categories"),
                    new MenuItem(L["Images"], "oi oi-image", "/catalog/images")
                }),
                new MenuItem(L["Settings"], "oi oi-cog", "/settings", new List<MenuItem>{
                    new MenuItem(L["EmailSettings"], "oi oi-envelope-closed", "/admin/email-settings"),
                    new MenuItem(L["ImageSettings"], "oi oi-image", "/settings/image")
                }),
                new MenuItem(L["Reports"], "oi oi-document", "/reports"),
                new MenuItem(L["Logout"], "oi oi-account-logout", "/Logout")
            };
        }
    }
}
