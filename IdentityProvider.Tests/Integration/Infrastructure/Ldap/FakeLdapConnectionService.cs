using System;
using IdentityProvider.Models.Entities;
using IdentityProvider.Services.Internal;
using SharedLensResources;

namespace IdentityProvider.Tests.Integration.Infrastructure.Ldap;

public class FakeLdapConnectionService : ILdapConnectionService
{
    // Credentials that skip LDAP process entirely, just returning good users
    public static User GetReggieRegular_UcLdap() => new()
    {
        Email = "reggie.regular@lens.com",
        GlobalLensRole = GlobalLensRole.User,
        FirstName = "Reggie",
        LastName = "Regular",
        UserName = "reggie.regular",
        EmployeeId = 1111111111,
        Created = DateTime.Now,
        IdentitySource = IdentitySource.Ldap
    };
    
    public static User GetSammieSysadmin_UcLdap() => new()
    {
        Email = "sammie.sysadmin@lens.com",
        GlobalLensRole = GlobalLensRole.SystemAdmin,
        FirstName = "Sammie",
        LastName = "Sysadmin",
        UserName = "sammie.sysadmin",
        EmployeeId = 999999999,
        Created = DateTime.Now,
        IdentitySource = IdentitySource.Ldap
    };

    // Credentials for testing the authentication itself
    public const string NotExistingUsername = "IDoNotExist";
    public const string PasswordThatIsAlwaysWrong = "IAmAlwaysWrong";

    public const string BasicUserUsername = "BasicUserUsername";
    public const string BasicUserPassword = "BasicUserPassword";

    public const string AdminUsername = "AdminUsername";
    public const string AdminPassword = "AdminPassword";

    public const string ForceConnectionError = "IForceAConnectionError";
    public const string ForceTimeoutError = "IForceATimeoutError";
    public const string ForceUnexpectedError = "IForceAnUnexpectedError";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="username">Username for logging in to UC Ldap</param>
    /// <param name="password">Password for logging in to UC Ldap</param>
    /// <returns>
    /// Success if correct credential combination is used, false if incorrect credential combination is used, and
    /// throws an exception if a username was used that is not included in the constants at the top of this file
    /// </returns>
    public LdapLoginAttemptResult AttemptLogin(string username, string password)
    {
        if (username == GetReggieRegular_UcLdap().UserName || username == GetSammieSysadmin_UcLdap().UserName)
        {
            return new LdapLoginAttemptResult
            {
                Status = LdapLoginAttemptResultStatus.Success,
                LensUser = username == GetReggieRegular_UcLdap().UserName ? GetReggieRegular_UcLdap() : GetSammieSysadmin_UcLdap()
            };
        }
        
        return username switch
        {
            NotExistingUsername => new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.CredentialError },
            ForceConnectionError => new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.ConnectionError },
            ForceTimeoutError => new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.TimeoutError },
            ForceUnexpectedError => new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.UnexpectedError },
            BasicUserUsername =>  password == BasicUserPassword
                ? new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.Success, LensUser = GetFakeLdapUser(BasicUserUsername)}
                : new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.CredentialError },
            AdminUsername => password == AdminPassword
                ? new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.Success, LensUser = GetFakeLdapUser(AdminUsername)}
                : new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.CredentialError },
            _ => throw new ArgumentException("Unexpected username used, LDAP mock is only configured " +
                                             "to use usernames like 'MockLdapConnectionService.BasicUserUsername'")
        };
    }

    /// <summary>
    /// Replaces the call that would usually try to contact the UCLdap server to search for a user by username.
    /// This method is configured to return either a BasicUser account, Admin account, or to throw an exception
    /// if there is some exceptional case encountered such as an incorrect username password combination.
    /// </summary>
    private static User GetFakeLdapUser(string requestUsername)
    {
        var ldapData = requestUsername switch
        {
            NotExistingUsername => throw new UnauthorizedAccessException("Username or password incorrect"),
            BasicUserUsername => LdapEntryDeserializer.DeserializeJsonFile("Resources/SampleLdapData/BasicUser.json"),
            AdminUsername => LdapEntryDeserializer.DeserializeJsonFile("Resources/SampleLdapData/Admin.json"),
            _ => throw new ArgumentException("Unexpected username used, LDAP mock is only configured " +
                                             "to use usernames like 'MockLdapConnectionService.BasicUserUsername'")
        };
        return LdapConnectionService.LdapEntryToLensUser(ldapData, requestUsername == AdminUsername);
    }
}