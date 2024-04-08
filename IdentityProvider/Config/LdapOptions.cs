namespace IdentityProvider.Config;

public class LdapOptions
{
    public string HostName { get; set; } = string.Empty;
    public int HostPort { get; set; } = 0;
    public bool UseSsl { get; set; } = true;
    public bool IgnoreCertificateVerification { get; set; } = false;
    public string UserQueryBase { get; set; } = string.Empty;
    public string DefaultAdminUserCodes { get; set; } = string.Empty;
    public string DomainName { get; set; } = string.Empty;
}