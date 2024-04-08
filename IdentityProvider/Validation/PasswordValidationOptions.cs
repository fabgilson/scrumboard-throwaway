using Microsoft.AspNetCore.Identity;

namespace IdentityProvider.Validation;

public class PasswordValidationOptions
{
    public static void DefaultPasswordPolicy(IdentityOptions options)
    {
        // Default Password settings.
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 2;
    }
}