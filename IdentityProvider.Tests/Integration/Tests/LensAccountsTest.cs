using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using IdentityProvider.Models.Entities;
using IdentityProvider.Tests.Integration.Infrastructure;
using IdentityProvider.Tests.Integration.Infrastructure.Ldap;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SharedLensResources;
using Xunit;

namespace IdentityProvider.Tests.Integration.Tests;

public class LensAccountsTestStartUp : BaseTestStartup
{
    public LensAccountsTestStartUp(IConfiguration configuration, IWebHostEnvironment webHost) 
        : base(configuration, webHost, "LensAccountsAuthenticationTestDatabase") { }
}

public class LensAccountsTest : BaseAuthenticationGrpcServiceTest<LensAccountsTestStartUp, LensAuthenticationService.LensAuthenticationServiceClient>
{
    public LensAccountsTest(CustomWebApplicationFactory<LensAccountsTestStartUp> customWebFactory) 
        : base(customWebFactory) { }
    
    private ILookupNormalizer LookupNormalizer => GetScopedService<ILookupNormalizer>();
    
    [Fact]
    public async Task CreateNewUserAccount_Unauthenticated_UnauthenticatedErrorReturned()
    {
        var action = async () => await UnauthenticatedClient.CreateNewLensAccountAsync(
            new CreateNewLensAccountRequest()
        );
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.Unauthenticated);
    }
    
    [Fact]
    public async Task CreateNewUserAccount_RegularUser_PermissionDeniedErrorReturned()
    {
        var action = async () => await RegularUserUcLdapAuthClient.CreateNewLensAccountAsync(
            new CreateNewLensAccountRequest()
        );
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.PermissionDenied);
    }

    private readonly CreateNewLensAccountRequest _createNewUserRequest = new()
    {
        UserName = "jimmy.space",
        Email = "jimmy.space@lens.com",
        Password = "V@lidPassw0rd",
        FirstName = "Jimmy",
        LastName = "Space",
        Role = GlobalLensRole.User
    };
    
    [Fact]
    public async Task CreateNewUserAccount_SystemAdmin_CreationSucceeds()
    {
        await using var dbContext = GetDatabaseContext();
        dbContext.Users.Should().NotContain(x => x.UserName == _createNewUserRequest.UserName);
        
        var response = await SystemAdminUcLdapAuthClient.CreateNewLensAccountAsync(_createNewUserRequest);
        response.Validation.IsSuccess.Should().BeTrue();
        var userInDb = dbContext.Users.First(x => x.UserName == _createNewUserRequest.UserName);
        
        response.UserResponse.Should().BeEquivalentTo(userInDb.ToUserResponse());
        userInDb.IdentitySource.Should().Be(IdentitySource.Lens);
        userInDb.GlobalLensRole.Should().Be(_createNewUserRequest.Role);
        userInDb.UserName.Should().Be(_createNewUserRequest.UserName);
        userInDb.NormalizedUserName.Should().Be(LookupNormalizer.NormalizeName(_createNewUserRequest.UserName));
    }
    
    [Fact]
    public async Task CreateNewUserAccount_UsernameAlreadyInUse_CorrectValidationErrorsReturned()
    {
        var inUseUserName = FakeLdapConnectionService.GetReggieRegular_UcLdap().UserName;
        await using var dbContext = GetDatabaseContext();
        dbContext.Users.Should().Contain(x => x.UserName == inUseUserName);
        
        var response = await SystemAdminUcLdapAuthClient.CreateNewLensAccountAsync(
            new CreateNewLensAccountRequest(_createNewUserRequest) { UserName = inUseUserName }
        );
        
        response.Validation.IsSuccess.Should().BeFalse();
        response.Validation.ValidationErrors.Should().Contain(new ValidationError
        {
            ErrorText = $"A user with the username {inUseUserName} already exists",
            FieldNames = { nameof(CreateNewLensAccountRequest.UserName) }
        });
    }
    
    [Fact]
    public async Task LogInWithLens_ValidCredentials_LoginSucceedsAndTokenIsGenerated()
    {
        var expectedUserInDb = await FindBennieRegular_LensId_Async();
        var loginReply = await RegularUserLensIdAuthClient.AuthenticateAsync(new LensAuthenticateRequest
        {
            Username = SampleDataHelper.GetBennieRegular_LensId().UserName,
            Password = SampleDataHelper.BennieRegularPassword,
            KeepLoggedIn = true
        });
        loginReply.Success.Should().BeTrue();
        loginReply.UserResponse.Should().BeEquivalentTo(expectedUserInDb.ToUserResponse());
        loginReply.Token.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task LogInWithLens_InvalidCredentials_LoginFails()
    {
        await SystemAdminUcLdapAuthClient.CreateNewLensAccountAsync(_createNewUserRequest);
        await using var dbContext = GetDatabaseContext();
        
        var loginReply = await RegularUserUcLdapAuthClient.AuthenticateAsync(new LensAuthenticateRequest
        {
            Username = SampleDataHelper.GetBennieRegular_LensId().UserName,
            Password = SampleDataHelper.BennieRegularPassword + "now_invalid",
            KeepLoggedIn = true
        });
        loginReply.Success.Should().BeFalse();
        loginReply.UserResponse.Should().BeNull();
    }

    public static readonly TheoryData<string, string[]> InvalidPasswordTheoryData = new()
    {
        { "MissingNumbers", new[] { new IdentityErrorDescriber().PasswordRequiresDigit().Description } },
        { "missing_upper_case", new[] { new IdentityErrorDescriber().PasswordRequiresUpper().Description } },
        { "MISSING_LOWER_CASE", new[] { new IdentityErrorDescriber().PasswordRequiresLower().Description } },
        // For origin of numbers below, see IdentityProvider.Validation.PasswordValidationOptions.cs
        { "aaaaaaaaa", new[] { new IdentityErrorDescriber().PasswordRequiresUniqueChars(2).Description } }, 
        { "short", new[] { new IdentityErrorDescriber().PasswordTooShort(8).Description } },
        // And one with everything
        { "@", new[] {
            new IdentityErrorDescriber().PasswordRequiresDigit().Description,
            new IdentityErrorDescriber().PasswordRequiresUpper().Description,
            new IdentityErrorDescriber().PasswordRequiresLower().Description,
            new IdentityErrorDescriber().PasswordRequiresUniqueChars(2).Description,
            new IdentityErrorDescriber().PasswordTooShort(8).Description
        } },
    };

    [Theory]
    [MemberData(nameof(InvalidPasswordTheoryData))]
    public async Task CreateNewUserAccount_InvalidPassword_PasswordValidationErrorsReturned(string password, string[] expectedPasswordErrorMessages)
    {
        var requestWithBadPassword = new CreateNewLensAccountRequest(_createNewUserRequest) { Password = password };
        await using var dbContext = GetDatabaseContext();
        dbContext.Users.Should().NotContain(x => x.UserName == requestWithBadPassword.UserName);
        var response = await SystemAdminUcLdapAuthClient.CreateNewLensAccountAsync(requestWithBadPassword);
        dbContext.Users.Should().NotContain(x => x.UserName == requestWithBadPassword.UserName);
        
        response.Validation.IsSuccess.Should().BeFalse();
        response.UserResponse.Should().BeNull();
        response.Validation.ValidationErrors.Should().Contain(
            expectedPasswordErrorMessages.Select(x =>
                new ValidationError { ErrorText = x, FieldNames = { nameof(CreateNewLensAccountRequest.Password) } }
            ));
    }
    
    [Theory]
    [MemberData(nameof(InvalidPasswordTheoryData))]
    public async Task ChangeOwnPassword_InvalidNewPassword_PasswordValidationErrorsReturned(string password, string[] expectedPasswordErrorMessages)
    {
        var startingPasswordHash = (await FindBennieRegular_LensId_Async()).PasswordHash;
        var requestWithBadPassword = new ChangeOwnPasswordRequest
        {
            CurrentPassword = SampleDataHelper.BennieRegularPassword,
            NewPassword = password,
            NewPasswordConfirm = password
        };
        
        var response = await RegularUserLensIdAuthClient.ChangeOwnPasswordAsync(requestWithBadPassword);
        response.IsSuccess.Should().BeFalse();
        response.ValidationErrors.Should().Contain(
        expectedPasswordErrorMessages.Select(x =>
            new ValidationError { ErrorText = x, FieldNames = { nameof(ChangeOwnPasswordRequest.NewPassword) } }
        ));
        (await FindBennieRegular_LensId_Async()).PasswordHash.Should().Be(startingPasswordHash);
    }
    
    [Fact]
    public async Task ChangeOwnPassword_NewPasswordsDontMatch_PasswordValidationErrorReturned()
    {
        var startingPasswordHash = (await FindBennieRegular_LensId_Async()).PasswordHash;
        var requestWithBadPassword = new ChangeOwnPasswordRequest
        {
            CurrentPassword = SampleDataHelper.BennieRegularPassword,
            NewPassword = "MyNewP@ssw0rd",
            NewPasswordConfirm = "S0meth1ngElse",
        };
        var response = await RegularUserLensIdAuthClient.ChangeOwnPasswordAsync(requestWithBadPassword);
        response.IsSuccess.Should().BeFalse();
        response.ValidationErrors.Should().Contain(
            new ValidationError
            {
                ErrorText = "Passwords do not match.", 
                FieldNames = { nameof(ChangeOwnPasswordRequest.NewPasswordConfirm) }
            }
        );
        (await FindBennieRegular_LensId_Async()).PasswordHash.Should().Be(startingPasswordHash);
    }
    
    [Fact]
    public async Task ChangeOwnPassword_CurrentPasswordIsWrong_PasswordValidationErrorReturned()
    {
        var startingPasswordHash = (await FindBennieRegular_LensId_Async()).PasswordHash;
        var requestWithBadPassword = new ChangeOwnPasswordRequest
        {
            CurrentPassword = SampleDataHelper.BennieRegularPassword + "now-wrong",
            NewPassword = "MyNewP@ssw0rd",
            NewPasswordConfirm = "MyNewP@ssw0rd",
        };
        var response = await RegularUserLensIdAuthClient.ChangeOwnPasswordAsync(requestWithBadPassword);
        response.IsSuccess.Should().BeFalse();
        response.ValidationErrors.Should().Contain(
            new ValidationError
            {
                ErrorText = "Incorrect password.", 
                FieldNames = { nameof(ChangeOwnPasswordRequest.CurrentPassword) }
            }
        );
        (await FindBennieRegular_LensId_Async()).PasswordHash.Should().Be(startingPasswordHash);
    }
    
    [Fact]
    public async Task ChangeOwnPassword_NotAuthenticated_UnauthenticatedErrorReturned()
    {
        var requestWithBadPassword = new ChangeOwnPasswordRequest 
            { CurrentPassword = "", NewPassword = "", NewPasswordConfirm = "" };
        var action = async () => await UnauthenticatedClient.ChangeOwnPasswordAsync(requestWithBadPassword);
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.Unauthenticated);
    }
    
    [Fact]
    public async Task ChangeOwnPassword_NonLensAccount_CorrectErrorReturned()
    {
        var requestForNonLensAccount = new ChangeOwnPasswordRequest
        {
            CurrentPassword = "not-used",
            NewPassword = "MyNewP@ssw0rd",
            NewPasswordConfirm = "MyNewP@ssw0rd",
        };
        var response = await RegularUserUcLdapAuthClient.ChangeOwnPasswordAsync(requestForNonLensAccount);
        response.IsSuccess.Should().BeFalse();
        response.Message.Should().Be("Your account is not managed by LENS Identity, and so you are unable to change your password here.");
    }
    
    [Fact]
    public async Task ChangeOwnPassword_EverythingIsFine_PasswordChangedSuccessfully()
    {
        var startingPasswordHash = (await FindBennieRegular_LensId_Async()).PasswordHash;
        var request = new ChangeOwnPasswordRequest
        {
            CurrentPassword = SampleDataHelper.BennieRegularPassword,
            NewPassword = "MyNewP@ssw0rd",
            NewPasswordConfirm = "MyNewP@ssw0rd",
        };
        var response = await RegularUserLensIdAuthClient.ChangeOwnPasswordAsync(request);
        response.IsSuccess.Should().BeTrue();
        response.ValidationErrors.Should().BeEmpty();
        (await FindBennieRegular_LensId_Async()).PasswordHash.Should().NotBe(startingPasswordHash);
    }
    
    [Fact]
    public async Task AdminForceChangePassword_Unauthenticated_UnauthenticatedErrorReturned()
    {
        var requestWithNotExistingAccount = new AdminForcePasswordChangeRequest 
            { UserId = 1, NewPassword = "MyNewP@ssw0rd" };
        var action = async () => await UnauthenticatedClient.AdminForcePasswordChangeAsync(requestWithNotExistingAccount);
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.Unauthenticated);
    }
    
    [Fact]
    public async Task AdminForceChangePassword_RegularUser_PermissionDeniedErrorReturned()
    {
        var requestWithNotExistingAccount = new AdminForcePasswordChangeRequest 
            { UserId = 1, NewPassword = "MyNewP@ssw0rd" };
        var action = async () => await RegularUserLensIdAuthClient.AdminForcePasswordChangeAsync(requestWithNotExistingAccount);
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task AdminForceChangePassword_NonLensAccount_CorrectErrorReturned()
    {
        var userHavingPasswordChanged = await FindReggieRegular_UcLdap_Async();
        var startingPasswordHash = userHavingPasswordChanged.PasswordHash;
        var requestForNonLensAccount = new AdminForcePasswordChangeRequest
        {
            UserId = userHavingPasswordChanged.Id,
            NewPassword = "MyNewP@ssw0rd"
        };
        var response = await SystemAdminUcLdapAuthClient.AdminForcePasswordChangeAsync(requestForNonLensAccount);
        response.IsSuccess.Should().BeFalse();
        response.ValidationErrors.Should().Contain(new ValidationError {
            ErrorText = "User account (and therefore password) is not managed by LENS",
            FieldNames = { nameof(AdminForcePasswordChangeRequest.UserId) }
        });
        (await FindReggieRegular_UcLdap_Async()).PasswordHash.Should().Be(startingPasswordHash);
    }
    
    [Fact]
    public async Task AdminForceChangePassword_AccountDoesNotExist_NotFoundErrorReturned()
    {
        var requestWithNotExistingAccount = new AdminForcePasswordChangeRequest 
            { UserId = 100, NewPassword = "MyNewP@ssw0rd" };
        var action = async () => await SystemAdminLensIdAuthClient.AdminForcePasswordChangeAsync(requestWithNotExistingAccount);
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.NotFound);
    }
    
    [Theory]
    [MemberData(nameof(InvalidPasswordTheoryData))]
    public async Task AdminForceChangePassword_InvalidNewPasswordGiven_CorrectErrorReturned(string password, string[] expectedPasswordErrorMessages)
    {
        var userHavingPasswordChanged = await FindBennieRegular_LensId_Async();
        var startingPasswordHash = userHavingPasswordChanged.PasswordHash;
        var requestWithBadPassword = new AdminForcePasswordChangeRequest
        {
            UserId = userHavingPasswordChanged.Id,
            NewPassword = password
        };
        
        var response = await SystemAdminUcLdapAuthClient.AdminForcePasswordChangeAsync(requestWithBadPassword);
        response.IsSuccess.Should().BeFalse();
        response.ValidationErrors.Should().Contain(
            expectedPasswordErrorMessages.Select(x =>
                new ValidationError { ErrorText = x, FieldNames = { nameof(AdminForcePasswordChangeRequest.NewPassword) } }
            ));
        (await FindBennieRegular_LensId_Async()).PasswordHash.Should().Be(startingPasswordHash);
    }
    
    [Fact]
    public async Task AdminForceChangePassword_SystemAdmin_PasswordChangedSuccessfully()
    {
        var userHavingPasswordChanged = await FindBennieRegular_LensId_Async();
        var startingPasswordHash = userHavingPasswordChanged.PasswordHash;
        var requestWithBadPassword = new AdminForcePasswordChangeRequest
        {
            UserId = userHavingPasswordChanged.Id,
            NewPassword = "MyNewP@ssw0rd",
        };
        
        var response = await SystemAdminUcLdapAuthClient.AdminForcePasswordChangeAsync(requestWithBadPassword);
        response.IsSuccess.Should().BeTrue();
        response.ValidationErrors.Should().BeEmpty();
        (await FindBennieRegular_LensId_Async()).PasswordHash.Should().NotBe(startingPasswordHash);
    }
}