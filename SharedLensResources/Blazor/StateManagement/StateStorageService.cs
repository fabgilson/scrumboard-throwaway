using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SharedLensResources.Blazor.StateManagement
{
    /// <summary>
    /// Service for managing the local state of the application.
    /// All the client local state of the application can accessed through this service
    /// </summary>
    public interface IStateStorageService
    {
        // Commonly stored / retrieved values
        Task SetBearerTokenAsync(string tokenValue, bool keepLoggedIn);
        Task<string> GetBearerTokenAsync();
        Task RemoveBearerTokenAsync();
    }

    /// <summary>
    /// Manages any client state that needs to be persisted. All state is to always be held in protected local storage, 
    /// except for the authentication bearer token which is only to be kept in local storage if the user has chosen to
    /// remain logged in (i.e 'Remember me' option on login page).
    /// </summary>
    public class StateStorageService : IStateStorageService
    {
        private static readonly string bearerTokenKey = "BEARER_TOKEN";

        private readonly IProtectedLocalStorageWrapper _protectedLocalStorage;
        private readonly IProtectedSessionStorageWrapper _protectedSessionStorage;

        private readonly ILogger<StateStorageService> _logger;

        public StateStorageService(
            IProtectedLocalStorageWrapper localStorageService,
            IProtectedSessionStorageWrapper sessionStorageService,
            ILogger<StateStorageService> logger
        )
        {
            _protectedLocalStorage = localStorageService;
            _protectedSessionStorage = sessionStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Sets the bearer token. If the user has chosen 'remember me' option in login, this should be set in 
        /// protected local storage rather than session.
        /// </summary>
        /// <param name="tokenValue"></param>
        /// <returns></returns>
        public async Task SetBearerTokenAsync(string tokenValue, bool keepLoggedIn)
        {
            await RemoveBearerTokenAsync();
            if (keepLoggedIn)
            {
                await _protectedLocalStorage.SetAsync("ScrumBoardBearerTokenPurpose", bearerTokenKey, tokenValue);
            }
            else
            {
                await _protectedSessionStorage.SetAsync("ScrumBoardBearerTokenPurpose", bearerTokenKey, tokenValue);
            }
        }

        /// <summary>
        /// First tries to read from local storage (in the case where a user is returning and previously chose
        /// to stay logged in), and then if it is not found, from session storage. If it is not found here 
        /// either, then the user must log in.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetBearerTokenAsync()
        {
            try
            {
                var token = await _protectedLocalStorage.GetAsync<string>("ScrumBoardBearerTokenPurpose",
                    bearerTokenKey);
                if (token != null) return token;

                token = await _protectedSessionStorage.GetAsync<string>("ScrumBoardBearerTokenPurpose", bearerTokenKey);
                return token;
            }
            catch (CryptographicException e)
            {
                _logger.LogWarning(e, "Invalid encryption for {}", bearerTokenKey);
                await RemoveBearerTokenAsync();
                return default;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        protected async Task<T> GetAsync<T>(string key)
        {
            try {
                return await _protectedLocalStorage.GetAsync<T>(key);
            } catch (CryptographicException e) {
                _logger.LogWarning(e, "Invalid encryption for {}", key);
                await _protectedLocalStorage.DeleteAsync(key);
                return default;
            }
        }

        protected async Task SetAsync(string key, object value)
        {
            await _protectedLocalStorage.SetAsync(key, value);
        }

        /// <summary>
        /// Clears the bearer token from both local and session storage
        /// </summary>
        public async Task RemoveBearerTokenAsync()
        {
            await _protectedLocalStorage.DeleteAsync(bearerTokenKey);
            await _protectedSessionStorage.DeleteAsync(bearerTokenKey);
        }
    }
}