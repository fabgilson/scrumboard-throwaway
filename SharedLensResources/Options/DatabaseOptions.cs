namespace SharedLensResources.Options;

public class DatabaseOptions
{
    public bool UseInMemory { get; set; } = false;
    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
}