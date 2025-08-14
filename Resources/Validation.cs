using System.Globalization;
using System.Resources;

namespace NetRoll
{
    // Wrapper exposing resx-based validation strings for DataAnnotations attributes
    public static class Validation
    {
    private static readonly ResourceManager RM = new ResourceManager("NetRoll.Validation", typeof(Validation).Assembly);

        public static string Required => RM.GetString("Required", CultureInfo.CurrentUICulture) ?? "The {0} field is required.";
        public static string EmailInvalid => RM.GetString("EmailInvalid", CultureInfo.CurrentUICulture) ?? "The Email field is not a valid e-mail address.";
        public static string StringLengthRange => RM.GetString("StringLengthRange", CultureInfo.CurrentUICulture) ?? "The {0} must be at least {2} and at max {1} characters long.";
        public static string PasswordMismatch => RM.GetString("PasswordMismatch", CultureInfo.CurrentUICulture) ?? "The password and confirmation password do not match.";
    }
}
