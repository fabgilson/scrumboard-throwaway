using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityProvider.Services.Internal;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Novell.Directory.Ldap;
using Google.Protobuf.WellKnownTypes;
using IdentityProvider.DataAccess;
using IdentityProvider.Models.Entities;
using IdentityProvider.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedLensResources;
using SharedLensResources.Extensions;
using Enum = System.Enum;

namespace IdentityProvider.Services.External;

[Authorize]
public class AuthenticationGrpcService : LensAuthenticationService.LensAuthenticationServiceBase
{
    private readonly ILdapConnectionService _ldapService;
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<AuthenticationGrpcService> _logger;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public AuthenticationGrpcService(
        IConfiguration configuration, 
        ILdapConnectionService ldapService,
        ILogger<AuthenticationGrpcService> logger, 
        UserManager<User> userManager,
        SignInManager<User> signInManager, 
        IDbContextFactory<DatabaseContext> dbContextFactory
    ) {
        _configuration = configuration;
        _ldapService = ldapService;
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc cref="LensAuthenticationService.LensAuthenticationServiceBase.CreateNewLensAccount"/>
    [Authorize(Roles = nameof(GlobalLensRole.SystemAdmin))]
    public override async Task<CreateNewLensAccountResponse> CreateNewLensAccount(CreateNewLensAccountRequest request, ServerCallContext context)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var newUser = request.ToEntity(out var validationState);

        if (dbContext.Users.Any(x => x.UserName == request.UserName))
        {
            validationState.AddValidationResult(new ValidationResult(
                $"A user with the username {request.UserName} already exists",
                new [] { nameof(request.UserName) }
            ));
        }

        await TryValidatePasswordAsync(validationState, request.Password, nameof(request.Password));
        if (!validationState.IsValid) return new CreateNewLensAccountResponse { Validation = validationState.ToValidationResponse() };
        
        validationState.WithIdentityErrorsForProperty(
            await _userManager.CreateAsync(newUser, request.Password),
            nameof(request.UserName)
        );
        var response = new CreateNewLensAccountResponse { Validation = validationState.ToValidationResponse() };
        if(validationState.IsValid) response.UserResponse = newUser.ToUserResponse();
        return response;
    }

    /// <inheritdoc cref="LensAuthenticationService.LensAuthenticationServiceBase.CheckAuthState"/>
    public override Task<ClaimsIdentityDto> CheckAuthState(Empty request, ServerCallContext context)
    {
        return Task.FromResult(((ClaimsIdentity)context.GetHttpContext().User.Identity).ToDto());
    }
    
    /// <inheritdoc cref="LensAuthenticationService.LensAuthenticationServiceBase.Authenticate"/>
    [AllowAnonymous]
    public override async Task<LensAuthenticationReply> Authenticate(LensAuthenticateRequest request, ServerCallContext context)
    {
        // Check if user already exists in the system
        var existingUser = await _userManager.FindByNameAsync(request.Username);

        if (existingUser is null || existingUser.IdentitySource is IdentitySource.Ldap)
        {
            return await AttemptLoginWithUcLdapAsync(request);
        }

        return await AttemptLoginWithLensIdentityAsync(request, existingUser);
    }
    

    /// <inheritdoc cref="LensAuthenticationService.LensAuthenticationServiceBase.ChangeOwnPassword"/>
    public override async Task<ValidationResponse> ChangeOwnPassword(ChangeOwnPasswordRequest request, ServerCallContext context)
    {
        var username = context.GetHttpContext().User.FindFirstValue(JwtRegisteredClaimNames.UniqueName);
        var user = await _userManager.FindByNameAsync(username);
        if (user.IdentitySource is not IdentitySource.Lens)
        {
            return new ValidationResponse
            {
                IsSuccess = false,
                Message = "Your account is not managed by LENS Identity, and so you are unable to change your password here."
            };
        }

        if (request.NewPassword != request.NewPasswordConfirm)
        {
            return new ValidationResponse
            {
                IsSuccess = false,
                ValidationErrors = { 
                    new ValidationError {
                        ErrorText = "Passwords do not match.",
                        FieldNames = { nameof(request.NewPasswordConfirm) } } 
                }
            };
        }

        var validationState = new ValidationState();
        await TryValidatePasswordAsync(validationState, request.NewPassword, nameof(request.NewPassword));
        validationState.WithIdentityErrorsForProperty(
            await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword),
            nameof(request.CurrentPassword)
        );

        return validationState.ToValidationResponse();
    }
    
    /// <inheritdoc cref="LensAuthenticationService.LensAuthenticationServiceBase.AdminForcePasswordChange"/>
    [Authorize(Roles = nameof(GlobalLensRole.SystemAdmin))]
    public override async Task<ValidationResponse> AdminForcePasswordChange(AdminForcePasswordChangeRequest request, ServerCallContext context)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new RpcException(new Status(StatusCode.NotFound, "No such user found with given ID"));
        if (user.IdentitySource is not IdentitySource.Lens)
        {
            return new ValidationResponse
            {
                IsSuccess = false,
                Message = "Only user accounts managed by the LENS IdP can have their passwords changed",
                ValidationErrors = { 
                    new ValidationError {
                        ErrorText = "User account (and therefore password) is not managed by LENS",
                        FieldNames = { nameof(request.UserId) } } 
                }
            };
        }
        var validationState = new ValidationState();
        await TryValidatePasswordAsync(validationState, request.NewPassword, nameof(request.NewPassword));
        if (!validationState.IsValid) return validationState.ToValidationResponse();
        
        await _userManager.RemovePasswordAsync(user);
        await _userManager.AddPasswordAsync(user, request.NewPassword);
        return validationState.ToValidationResponse();
    }

    /// <summary>
    /// Validates some given password against the IdP password policy, and appends any issues with validation
    /// to some ValidationState object, citing the given property name.
    /// </summary>
    /// <param name="validationState">Existing validation state where new issues can be added</param>
    /// <param name="password">Password to validate against IdP password policy</param>
    /// <param name="propertyName">Name of property to cite for any errors encountered</param>
    private async Task TryValidatePasswordAsync(ValidationState validationState, string password, string propertyName)
    {
        var passwordValidator = new PasswordValidator<User>();
        validationState.WithIdentityErrorsForProperty(
            await passwordValidator.ValidateAsync(_userManager, null, password), 
            propertyName
        );
    }
    
    /// <summary>
    /// Attempts to authenticate some provided credentials against the custom LENS user authentication store.
    /// </summary>
    /// <param name="request">Request containing sign-in credentials</param>
    /// <param name="user">LENS user entity matching the specified username</param>
    /// <returns>LensAuthenticationReply describing the result of the authentication attempt</returns>
    private async Task<LensAuthenticationReply> AttemptLoginWithLensIdentityAsync(LensAuthenticateRequest request, User user)
    {
        var signInAttempt = await _signInManager.PasswordSignInAsync(user, request.Password, request.KeepLoggedIn, false);
        if (!signInAttempt.Succeeded)
            return new LensAuthenticationReply
            {
                Message = "Log in attempt unsuccessful, please check your username and password and try again.",
                Token = "",
                Success = false
            };
        var token = await GenerateJwtTokenForUserAsync(user.UserName);
        return SuccessfulLoginAttemptReply(user, "Successfully logged in with LENS ID!", token);
    }

    /// <summary>
    /// Attempts to authenticate some provided credentials against the University of Canterbury LDAP server.
    /// </summary>
    /// <param name="request">Request containing sign-in credentials</param>
    /// <returns>LensAuthenticationReply describing the result of the authentication attempt</returns>
    private async Task<LensAuthenticationReply> AttemptLoginWithUcLdapAsync(LensAuthenticateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return FailedLoginAttemptReply("Username and password may not be blank.");
        
        var loginResult = _ldapService.AttemptLogin(request.Username, request.Password);
        if (loginResult.Status != LdapLoginAttemptResultStatus.Success) return FailedLoginAttemptReply(loginResult.Status.GetMessage());

        // If user already exists, pull it from the database, if not, add it to the database
        User user;
        try
        {
            user = await GetUcLdapUserOrCreateIfNotExistsAsync(request.Username, loginResult.LensUser);
        } catch (LdapException e) {
            _logger.LogError(e, "Failed to search for user");
            return new LensAuthenticationReply { 
                Message = "Unable to read user information from UC identity server, please try again later", 
                Token = "", 
                Success = false 
            };
        }
        
        var token = await GenerateJwtTokenForUserAsync(user.UserName);

        return new LensAuthenticationReply { 
            Message = loginResult.Status.GetMessage(), 
            Token = token, 
            Success = true,
            UserResponse = user.ToUserResponse()
        };
    }
    
    private static LensAuthenticationReply SuccessfulLoginAttemptReply(User user, string successMessage, string token) => new()
    {
        Message = successMessage, 
        Token = token, 
        Success = true,
        UserResponse = user.ToUserResponse()
    };

    private static LensAuthenticationReply FailedLoginAttemptReply(string failMessage) => new()
    {
        Message = failMessage, 
        Token = "", 
        Success = false
    };

    /// <summary>
    /// For some LdapEntry: if there is already a corresponding user entity in the database, return it; if there is 
    /// no corresponding user entity found, add it to the database and then return it.
    /// </summary>
    /// <param name="username">Username of user account</param>
    private async Task<User> GetUcLdapUserOrCreateIfNotExistsAsync(string username, User lensUser)
    {
        var existingUser = await _userManager.FindByNameAsync(username);
        if (existingUser is not null) return existingUser;
        await _userManager.CreateAsync(lensUser);
        return lensUser;
    }

    /// <summary>
    /// For some username, finds the user in the database and generates a new Jwt Bearer token for them.
    /// Included in this token is the session information they need to authenticate themselves to this server,
    /// and also some basic information about the user (e.g name, role-type, ...).
    /// </summary>
    /// <param name="username">Username of user account for which to generate a JWT bearer token</param>
    /// <param name="securityStamp">Security stamp that may be changed (in IdP state) to revoke a JWT token.</param>
    /// <returns>String encoded JWT token</returns>
    private async Task<string> GenerateJwtTokenForUserAsync(string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration.GetValue<string>("SigningKey"));

        var claims = new ClaimsIdentity();
        claims.AddClaim(new Claim(JwtRegisteredClaimNames.UniqueName, username));
        claims.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, username));

        var user = await _userManager.FindByNameAsync(username);
        claims.AddClaim(new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()));
        claims.AddClaim(new Claim("name", $"{user.FirstName} {user.LastName}"));
        claims.AddClaim(new Claim(ClaimTypes.Role, Enum.GetName(typeof(GlobalLensRole), user.GlobalLensRole) ?? string.Empty));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        }; var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}