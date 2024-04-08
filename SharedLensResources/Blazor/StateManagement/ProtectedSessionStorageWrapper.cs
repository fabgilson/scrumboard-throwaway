using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace SharedLensResources.Blazor.StateManagement
{
    /// <summary>
    /// The ProtectedBrowserStorage implementations do not implement any interface and are sealed classes making them
    /// impossible to mock. Therefore we define our own interface and create wrapper classes for them to allow mocking.
    /// </summary>
    public interface IProtectedSessionStorageWrapper
    {
        Task SetAsync(string key, object value);
        Task SetAsync(string purpose, string key, object value);
        Task<T> GetAsync<T>(string key);
        Task<T> GetAsync<T>(string purpose, string key);
        Task DeleteAsync(string key);
    }

    /// <summary>
    /// Wrapper class around ProtectedSessionStorage that allows it to be mocked
    /// </summary>
    public class ProtectedSessionStorageWrapper : IProtectedSessionStorageWrapper
    {
        private readonly ProtectedSessionStorage _protectedSessionStorage;

        public ProtectedSessionStorageWrapper(ProtectedSessionStorage protectedSessionStorage)
        {
            _protectedSessionStorage = protectedSessionStorage;
        }

        public async Task DeleteAsync(string key)
        {
            await _protectedSessionStorage.DeleteAsync(key);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            return (await _protectedSessionStorage.GetAsync<T>(key)).Value;
        }

        public async Task<T> GetAsync<T>(string purpose, string key)
        {
            return (await _protectedSessionStorage.GetAsync<T>(purpose, key)).Value;
        }

        public async Task SetAsync(string key, object value)
        {
            await _protectedSessionStorage.SetAsync(key, value);
        }

        public async Task SetAsync(string purpose, string key, object value)
        {
            await _protectedSessionStorage.SetAsync(purpose, key, value);
        }
    }
}
