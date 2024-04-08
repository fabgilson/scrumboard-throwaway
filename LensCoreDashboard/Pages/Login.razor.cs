using Microsoft.AspNetCore.Components;
using SharedLensResources;
using SharedLensResources.Authentication;
using SharedLensResources.Blazor.StateManagement;

namespace LensCoreDashboard.Pages
{
    public partial class Login
    {
        private string _errorText;

        private LensAuthenticateRequest AuthenticateRequest { get; set; } = new ();

        [Inject]
        protected ILogger<Login> Logger { get; set; }

        [Inject]
        protected IAuthenticationService AuthenticationService { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected IStateStorageService StateStorageService { get; set; }

        /// <summary>
        /// Authenticate with IdentityProvider given the provided credentials. If login attempt is successful, take
        /// user to the homepage, if not, show the error message.
        /// </summary>
        private async Task AttemptLogin()
        {
            var loginAttempt = await AuthenticationService.AttemptLogin(AuthenticateRequest);
            if (loginAttempt.Success)
            {
                Logger.LogInformation("User (Id={userId}) logged in successfully", loginAttempt.UserResponse.Id);
                NavigationManager.NavigateTo("", true);
            }
            else
            {
                Logger.LogInformation("User failed to login");
                _errorText = loginAttempt.Message;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Property for when to show the error label (whenever the error-text is not blank)
        /// </summary>
        private bool HideError => string.IsNullOrWhiteSpace(_errorText);
    }
}
