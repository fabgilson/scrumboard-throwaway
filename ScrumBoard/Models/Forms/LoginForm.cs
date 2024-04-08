using System.ComponentModel.DataAnnotations;
using SharedLensResources;

namespace ScrumBoard.Models.Forms;

public class LoginForm
{
    public LoginForm() {}
        
    public LoginForm(bool keepLoggedIn) {
        KeepLoggedIn = keepLoggedIn;
    }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Username is required")]
    public string Username { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required")]
    public string Password { get; set; }
    
    public bool KeepLoggedIn { get; set; }

    public LensAuthenticateRequest AsLensAuthenticateRequest => new()
    {
        KeepLoggedIn = KeepLoggedIn,
        Username = Username,
        Password = Password
    };
}
