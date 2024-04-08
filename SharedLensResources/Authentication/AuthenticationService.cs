using System.Security.Claims;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using SharedLensResources.Blazor.StateManagement;
using SharedLensResources.Extensions;

namespace SharedLensResources.Authentication
{
    public interface IAuthenticationService
    {
        Task<LensAuthenticationReply> AttemptLogin(LensAuthenticateRequest authenticateRequest);
        Task<AuthenticationState> GetCurrentAuthenticationStateAsync();
        Task<ClaimsIdentity> GetClaimsIdentityForBearerTokenAsync(string overrideBearerToken = null);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IStateStorageService _stateStorageService;
        private readonly LensAuthenticationService.LensAuthenticationServiceClient _lensAuthClient;

        public AuthenticationService(
            ILogger<AuthenticationService> logger,
            IStateStorageService stateStorageService, 
            LensAuthenticationService.LensAuthenticationServiceClient lensAuthClient)
        {
            _logger = logger;
            _stateStorageService = stateStorageService;
            _lensAuthClient = lensAuthClient;
        }

        /// <summary>
        /// Attempt to log in to auth provider with given username and password. On a success, store the bearer token
        /// in state storage. 
        /// </summary>
        /// <param name="authenticateRequest">Request object containing details of account to attempt login with</param>
        /// <returns>True on success, false otherwise</returns>
        public async Task<LensAuthenticationReply> AttemptLogin(LensAuthenticateRequest authenticateRequest)
        {
            try 
            {
                var loginReply = await _lensAuthClient.AuthenticateAsync(authenticateRequest);
                if (string.IsNullOrEmpty(loginReply.Token)) return new LensAuthenticationReply { Message = loginReply.Message, Success = false };
                await _stateStorageService.SetBearerTokenAsync(loginReply.Token, authenticateRequest.KeepLoggedIn);
                return loginReply;
            } 
            catch (RpcException e) 
            {
                if (e.StatusCode != StatusCode.Unavailable) throw;
                _logger.LogError(e, "Login Failed: Identity provider unavailable");   
                return new LensAuthenticationReply { Message = "Identity provider unavailable", Success = false };
            }            
        }

        /// <summary>
        /// Query the auth provider for the current authentication state of user with current bearer token
        /// </summary>
        /// <returns>Auth state of current user</returns>
        public async Task<AuthenticationState> GetCurrentAuthenticationStateAsync()
        {
            try
            {
                var authStateReply = await _lensAuthClient.CheckAuthStateAsync(new Empty());
                var claimsIdentity = authStateReply.FromDto();

                var user = new ClaimsPrincipal(claimsIdentity);
                return new AuthenticationState(user);

            }
            catch (RpcException e)
            {          
                if (e.StatusCode is StatusCode.Unauthenticated or StatusCode.Unavailable)
                {
                    return new AuthenticationState(new ClaimsPrincipal());
                }
                throw;
            }
        }

        public async Task<ClaimsIdentity> GetClaimsIdentityForBearerTokenAsync(string overrideBearerToken=null)
        {
            var claimsIdentityDto = await _lensAuthClient.CheckAuthStateAsync(
                new Empty(), 
                new Metadata { { "Authorization", overrideBearerToken ?? "" } }
            );
            return claimsIdentityDto.FromDto();
        }
    }
}
