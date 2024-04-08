namespace ScrumBoard.Config;

public class StudentGuideOptions
{
    public bool Enabled { get; set; } = false;
    public string ContentPath { get; set; } = string.Empty;
    public string GitlabZipPath { get; set; } = string.Empty;
    public string GitlabTagPath { get; set; } = string.Empty;
    public string GitlabAccessToken { get; set; } = string.Empty;
}