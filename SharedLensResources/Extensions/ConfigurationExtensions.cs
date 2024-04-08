using Microsoft.Extensions.Configuration;

namespace SharedLensResources.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Gets the configured base path of the application if it has one, e.g if hosted behind a reverse-proxy.
    /// Any trailing forward slashes are removed.
    /// </summary>
    /// <returns>Base path if one is configured, otherwise null</returns>
    public static string GetAppBasePath(this IConfiguration configuration)
    {
        var val = configuration.GetValue<string>("AppBaseUrl");
        val = val?.TrimEnd('/');
        return string.IsNullOrEmpty(val) ? null : val;
    }

    public static string GetIdentityProviderUrl(this IConfiguration configuration)
    {
        return configuration.GetValue<string>("IdentityProviderUrl");
    }
}