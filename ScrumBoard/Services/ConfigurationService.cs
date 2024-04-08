using Microsoft.Extensions.Configuration;
using ScrumBoard.Config;

namespace ScrumBoard.Services;

public interface IConfigurationService
{
    public bool FeedbackFormsEnabled { get; }
    public bool WebhooksEnabled { get; }
    public bool SeedDataEnabled { get; }
        
    public bool StudentGuideEnabled { get; }
    public string StudentGuideContentPath { get; }
    public string StudentGuideGitlabZipPath { get; }
    public string StudentGuideGitlabTagPath { get; }
    public string StudentGuideGitlabAccessToken { get; }

    public bool LiveUpdatesSkipSslValidation { get; }
    
    public string GetBaseUrl();
}
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly StudentGuideOptions _studentGuideOptions = new();
    private readonly LiveUpdateOptions _liveUpdateOptions = new();

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
        _configuration.GetSection("StudentGuide").Bind(_studentGuideOptions);
        _configuration.GetSection("LiveUpdate").Bind(_liveUpdateOptions);
    }

    public bool FeedbackFormsEnabled => _configuration.GetValue<bool>("EnableFeedbackForms");

    public bool WebhooksEnabled => _configuration.GetValue<bool>("EnableWebHooks");

    public bool SeedDataEnabled => _configuration.GetValue<bool>("EnableSeedData");

    public bool StudentGuideEnabled => _studentGuideOptions.Enabled;
    public string StudentGuideContentPath => _studentGuideOptions.ContentPath;
    public string StudentGuideGitlabZipPath => _studentGuideOptions.GitlabZipPath;
    public string StudentGuideGitlabTagPath => _studentGuideOptions.GitlabTagPath;
    public string StudentGuideGitlabAccessToken => _studentGuideOptions.GitlabAccessToken;

    public bool LiveUpdatesSkipSslValidation => _liveUpdateOptions.IgnoreSslValidation;
    
    public string GetBaseUrl() => _configuration.GetValue("AppBaseUrl", "");

}