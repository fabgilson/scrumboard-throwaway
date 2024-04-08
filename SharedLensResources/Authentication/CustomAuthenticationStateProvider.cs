using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace SharedLensResources.Authentication
{
    /// <summary>
    /// Communicates with the relevant "AuthenticationProvider" application to determine the authentication
    /// and authorization of the user.
    /// </summary>
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IAuthenticationService _authenticationService;

        public CustomAuthenticationStateProvider(
            IAuthenticationService authenticationService
        )
        {
            _authenticationService = authenticationService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return await _authenticationService.GetCurrentAuthenticationStateAsync();
        }
    }
}