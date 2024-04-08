using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IdentityProvider.Config;
using IdentityProvider.Models.Entities;
using IdentityProvider.Models.Ldap;
using Novell.Directory.Ldap;
using SharedLensResources;

namespace IdentityProvider.Services.Internal
{
    public interface ILdapConnectionService
    {
        LdapLoginAttemptResult AttemptLogin(string username, string password);
    }

    public struct LdapLoginAttemptResult
    {
        public LdapLoginAttemptResultStatus Status { get; set; }
        public User LensUser { get; set; }
    }
    
    public enum LdapLoginAttemptResultStatus
    {
        Success,
        ConnectionError,
        CredentialError,
        TimeoutError,
        UnexpectedError
    }

    public static class LdapLoginAttemptResultExtensions
    {
        public const string SuccessMessage = "Successfully logged in!";
        public const string ConnectionErrorMessage = "Error connecting to UC Identity server, please try again later.";
        public const string CredentialErrorMessage = "Incorrect username or password.";
        public const string TimeoutErrorMessage = "Took too long to get a response from UC identity server, please try again later.";
        public const string UnexpectedErrorMessage = "Something went wrong on our end, please try again later.";

        public static string GetMessage(this LdapLoginAttemptResultStatus result)
        {
            return result switch
            {
                LdapLoginAttemptResultStatus.Success => SuccessMessage,
                LdapLoginAttemptResultStatus.ConnectionError => ConnectionErrorMessage,
                LdapLoginAttemptResultStatus.CredentialError => CredentialErrorMessage,
                LdapLoginAttemptResultStatus.TimeoutError => TimeoutErrorMessage,
                LdapLoginAttemptResultStatus.UnexpectedError => UnexpectedErrorMessage,
                _ => throw new NotImplementedException()
            };
        }
    }

    public class LdapConnectionService : ILdapConnectionService
    {
        private readonly LdapOptions _ldapOptions;
        private readonly ILogger<LdapConnectionService> _logger;

        public LdapConnectionService(IConfiguration configuration, ILogger<LdapConnectionService> logger)
        {
            _logger = logger;
            _ldapOptions = new LdapOptions();
            configuration.GetSection("Ldap").Bind(_ldapOptions);
        }

        /// <summary>
        /// Attempt to log in with some given credentials. Based on one of many situations that could occur,
        /// (successful login, invalid credentials, timeout, connection error, ...) return the appropriate
        /// LoginAttemptResult.
        /// </summary>
        /// <param name="username">Username to attempt to log in, should not be blank</param>
        /// <param name="password">Password with which to log in, should not be blank</param>
        /// <returns></returns>
        public LdapLoginAttemptResult AttemptLogin(string username, string password)
        {
            var ldapConnectionOptions = ConfigureLdapConnectionOptions();
            var connection = CreateAndConfigureLdapConnection(ldapConnectionOptions);
            
            try
            {
                connection.Bind($"{username}@{_ldapOptions.DomainName}", password);
                _logger.LogInformation("Successfully bound to LDAP!");
                return SearchUserInDirectory(username, connection);
            }
            catch (LdapException ex)
            {
                return HandleLdapException(ex);
            }
            catch (TimeoutException ex)
            {
                return HandleTimeoutException(ex);
            }
            catch (Exception ex)
            {
                return HandleGeneralException(ex);
            }
        }
        
        /// <summary>
        /// Configures the LDAP connection options based on the application settings.
        /// </summary>
        private LdapConnectionOptions ConfigureLdapConnectionOptions()
        {
            var ldapConnectionOptions = new LdapConnectionOptions();
            if (_ldapOptions.UseSsl)
                ldapConnectionOptions = ldapConnectionOptions.UseSsl();

            if (_ldapOptions.IgnoreCertificateVerification)
                ldapConnectionOptions.ConfigureRemoteCertificateValidationCallback((_, _, _, _) => true);

            return ldapConnectionOptions;
        }
        
        /// <summary>
        /// Creates and configures an instance of LdapConnection.
        /// </summary>
        private LdapConnection CreateAndConfigureLdapConnection(LdapConnectionOptions options)
        {
            var connection = new LdapConnection(options);
            connection.SearchConstraints.ReferralFollowing = true;
            connection.Connect(_ldapOptions.HostName, _ldapOptions.HostPort);
            return connection;
        }
        
        /// <summary>
        /// Handles exceptions specific to the LDAP operation.
        /// </summary>
        private LdapLoginAttemptResult HandleLdapException(LdapException ex)
        {
            _logger.LogError(ex, "Failed to bind with given network credentials");
            
            switch (ex.ResultCode)
            {
                case LdapException.InvalidCredentials:
                    return new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.CredentialError };

                case LdapException.ConnectError:
                    return new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.ConnectionError };

                default:
                    _logger.LogError(ex, "Unexpected LDAP result code when attempting to bind to UC LDAP server");
                    return new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.UnexpectedError };
            }
        }
        
        /// <summary>
        /// Handles timeout exceptions during the LDAP operation.
        /// </summary>
        private LdapLoginAttemptResult HandleTimeoutException(Exception ex)
        {
            _logger.LogCritical(ex, "Timeout exceeded trying to bind to UC LDAP server");
            return new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.TimeoutError };
        }

        /// <summary>
        /// Handles general exceptions that are not specifically related to the LDAP operation.
        /// </summary>
        private LdapLoginAttemptResult HandleGeneralException(Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error occurred when attempting to bind to LDAP server with given credentials");
            return new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.UnexpectedError };
        }
        
        /// <summary>
        /// Searches for the user in the LDAP directory and constructs the result.
        /// </summary>
        private LdapLoginAttemptResult SearchUserInDirectory(string username, ILdapConnection connection)
        {
            _logger.LogInformation("Searching for user with username: {Username}", username);
            try
            {
                var ldapSearchResults = connection.Search(
                    _ldapOptions.UserQueryBase,
                    LdapConnection.ScopeSub,
                    $"(&(objectClass=user)(cn={username}))",
                    new[] { "*" },
                    false
                );

                if (!ldapSearchResults.HasMore())
                {
                    _logger.LogError("Search found no users with given username");
                    throw new KeyNotFoundException($"No user with username '{username}' could be found");
                }

                var user = ldapSearchResults.Next();
                return new LdapLoginAttemptResult
                {
                    Status = LdapLoginAttemptResultStatus.Success,
                    LensUser = LdapEntryToLensUser(user, _ldapOptions.DefaultAdminUserCodes.Split(',').Contains(username))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during search operation");
                return new LdapLoginAttemptResult { Status = LdapLoginAttemptResultStatus.UnexpectedError };
            }
        }

        public static User LdapEntryToLensUser(LdapEntry userLdapData, bool isSystemAdmin=false)
        {
            return new User
            {
                Email = userLdapData.GetAttribute("mail").StringValue,
                FirstName = userLdapData.GetAttribute("givenname").StringValue,
                LastName = userLdapData.GetAttribute("sn").StringValue,
                UserName = userLdapData.GetAttribute("cn").StringValue,
                Created = DateTime.Now,
                GlobalLensRole = isSystemAdmin ? GlobalLensRole.SystemAdmin : GlobalLensRole.User,
                EmployeeId = userLdapData.GetAttribute("uidNumber").ToSingletonInteger(),
                IdentitySource = IdentitySource.Ldap
            };
        }
    }
}