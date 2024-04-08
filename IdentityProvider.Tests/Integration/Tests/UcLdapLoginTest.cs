using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityProvider.Services.Internal;
using IdentityProvider.Tests.Integration.Infrastructure;
using IdentityProvider.Tests.Integration.Infrastructure.Ldap;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SharedLensResources;
using Xunit;

namespace IdentityProvider.Tests.Integration.Tests;

public class UcLdapLoginTestStartup : BaseTestStartup
{
    public UcLdapLoginTestStartup(IConfiguration configuration, IWebHostEnvironment webHost) 
        : base(configuration, webHost, "UcLdapLoginAuthenticationTestDatabase") { }
}

public class UcLdapLoginTest : BaseAuthenticationGrpcServiceTest<UcLdapLoginTestStartup, LensAuthenticationService.LensAuthenticationServiceClient>
{
    public UcLdapLoginTest(CustomWebApplicationFactory<UcLdapLoginTestStartup> customWebFactory) 
        : base(customWebFactory) { }
    
    /// <summary>
    /// Util method for sending an authentication request with given username and password
    /// Nothing fancy, just avoids duplicating code throughout tests below
    /// </summary>
    private async Task<LensAuthenticationReply> LogInWithCredentials(string username, string password)
    {
        var request = new LensAuthenticateRequest
        {
            Username = username,
            Password = password
        };
        return await UnauthenticatedClient.AuthenticateAsync(request);
    }

    [Fact]
    public async Task LoginWithLdap_UserDoesNotExist_Fails()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.NotExistingUsername, FakeLdapConnectionService.PasswordThatIsAlwaysWrong);

        Assert.False(reply.Success);
        Assert.True(string.IsNullOrEmpty(reply.Token));
        Assert.False(string.IsNullOrEmpty(reply.Message));
    }

    [Fact]
    public async Task LoginWithLdap_UserExistsButIncorrectPassword_Fails()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.BasicUserUsername, FakeLdapConnectionService.PasswordThatIsAlwaysWrong);

        Assert.False(reply.Success);
        Assert.True(string.IsNullOrEmpty(reply.Token));
        Assert.False(string.IsNullOrEmpty(reply.Message));
    }

    [Fact]
    public async Task LoginWithLdap_GoodCredentials_Succeeds()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.BasicUserUsername, FakeLdapConnectionService.BasicUserPassword);

        Assert.True(reply.Success);
        Assert.False(string.IsNullOrEmpty(reply.Token));
    }

    [Fact]
    public async Task LoginWithLdap_GoodCredentials_ReturnsValidJwtToken()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.BasicUserUsername, FakeLdapConnectionService.BasicUserPassword);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(reply.Token);

        FakeLdapConnectionService.BasicUserUsername.ToUpper().Should().Be(token.Subject.ToUpper());
    }

    [Fact]
    public async Task LoginWithLdap_BasicUserCredentials_HasOnlyUserRoleEncodedInToken()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.BasicUserUsername, FakeLdapConnectionService.BasicUserPassword);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(reply.Token);

        Assert.Single(token.Claims.Where(c => c.Type == "role"));
        Assert.Equal(Enum.GetName(typeof(GlobalLensRole), GlobalLensRole.User), token.Claims.First(c => c.Type == "role").Value);
    }

    [Fact]
    public async Task LoginWithLdap_BasicUserCredentials_HasCorrectNameEncodedInToken()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.BasicUserUsername, FakeLdapConnectionService.BasicUserPassword);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(reply.Token);

        Assert.Single(token.Claims.Where(c => c.Type == "name"));
        Assert.Equal($"Basic User", token.Claims.First(c => c.Type == "name").Value);
    }

    [Fact]
    public async Task LoginWithLdap_BasicUserCredentials_HasCorrectName()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.BasicUserUsername, FakeLdapConnectionService.BasicUserPassword);
        reply.UserResponse.FirstName.Should().Be("Basic"); // Name defined in BasicUser.json sample data
        reply.UserResponse.LastName.Should().Be("User"); 
    }

    [Fact]
    public async Task LoginWithLdap_BasicUserCredentials_HasNonEmptyId()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.BasicUserUsername, FakeLdapConnectionService.BasicUserPassword);
        reply.UserResponse.Id.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task LoginWithLdap_AdminCredentials_HasOnlyAdminRoleEncodedInToken()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.AdminUsername, FakeLdapConnectionService.AdminPassword);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(reply.Token);

        Assert.Single(token.Claims.Where(c => c.Type == "role"));
        Assert.Equal(Enum.GetName(typeof(GlobalLensRole), GlobalLensRole.SystemAdmin), token.Claims.First(c => c.Type == "role").Value);
    }

    [Fact]
    public async Task LoginWithLdap_AdminCredentials_HasCorrectNameEncodedInToken()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.AdminUsername, FakeLdapConnectionService.AdminPassword);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(reply.Token);

        Assert.Single(token.Claims.Where(c => c.Type == "name"));
        Assert.Equal($"Admin Admin", token.Claims.First(c => c.Type == "name").Value);
    }

    [Fact]
    public async Task LoginWithLdap_AdminCredentials_HasCorrectName()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.AdminUsername, FakeLdapConnectionService.AdminPassword);
        reply.UserResponse.FirstName.Should().Be("Admin"); // Name defined in Admin.json sample data
        reply.UserResponse.LastName.Should().Be("Admin"); 
    }

    [Fact]
    public async Task LoginWithLdap_AdminCredentials_HasNonZeroId()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.AdminUsername, FakeLdapConnectionService.AdminPassword);
        reply.UserResponse.Id.Should().BeGreaterThanOrEqualTo(1);
    }

    [Theory]
    [InlineData("", FakeLdapConnectionService.PasswordThatIsAlwaysWrong)]
    [InlineData("    ", FakeLdapConnectionService.PasswordThatIsAlwaysWrong)]
    [InlineData("\t", FakeLdapConnectionService.PasswordThatIsAlwaysWrong)]
    public async Task LoginWithLdap_BlankOrWhitespaceUsername_IsHandledGracefully(string username, string password)
    {
        var exception = await Record.ExceptionAsync(() => LogInWithCredentials(username, password));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("", FakeLdapConnectionService.PasswordThatIsAlwaysWrong)]
    [InlineData("    ", FakeLdapConnectionService.PasswordThatIsAlwaysWrong)]
    [InlineData("\t", FakeLdapConnectionService.PasswordThatIsAlwaysWrong)]
    public async Task LoginWithLdap_BlankOrWhitespaceUsername_AppropriateMessageAndSuccessCodeReturned(string username, string password)
    {
        var reply = await LogInWithCredentials(username, password);
        Assert.Equal("Username and password may not be blank.", reply.Message);
        Assert.False(reply.Success);
    }

    [Theory]
    [InlineData(FakeLdapConnectionService.BasicUserUsername, "")]
    [InlineData(FakeLdapConnectionService.BasicUserUsername, "    ")]
    [InlineData(FakeLdapConnectionService.BasicUserUsername, "\t")]
    public async Task LoginWithLdap_BlankOrWhitespacePassword_IsHandledGracefully(string username, string password)
    {
        var exception = await Record.ExceptionAsync(() => LogInWithCredentials(username, password));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(FakeLdapConnectionService.BasicUserUsername, "")]
    [InlineData(FakeLdapConnectionService.BasicUserUsername, "    ")]
    [InlineData(FakeLdapConnectionService.BasicUserUsername, "\t")]
    public async Task LoginWithLdap_BlankOrWhitespacePassword_AppropriateMessageAndSuccessCodeReturned(string username, string password)
    {
        var reply = await LogInWithCredentials(username, password);
        Assert.Equal("Username and password may not be blank.", reply.Message);
        Assert.False(reply.Success);
    }

    [Fact]
    public async Task LoginWithLdap_ConnectionErrorOccurs_IsHandledGracefully()
    {
        var exception = await Record.ExceptionAsync(() =>
            LogInWithCredentials(
                FakeLdapConnectionService.ForceConnectionError,
                FakeLdapConnectionService.PasswordThatIsAlwaysWrong
            )
        );
        Assert.Null(exception);
    }

    [Fact]
    public async Task LoginWithLdap_ConnectionErrorOccurs_AppropriateMessageAndSuccessCodeReturned()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.ForceConnectionError, FakeLdapConnectionService.PasswordThatIsAlwaysWrong);
        Assert.Equal(LdapLoginAttemptResultExtensions.ConnectionErrorMessage, reply.Message);
        Assert.False(reply.Success);
    }

    [Fact]
    public async Task LoginWithLdap_TimeoutErrorOccurs_IsHandledGracefully()
    {
        var exception = await Record.ExceptionAsync(() =>
            LogInWithCredentials(
                FakeLdapConnectionService.ForceTimeoutError,
                FakeLdapConnectionService.PasswordThatIsAlwaysWrong
            )
        );
        Assert.Null(exception);
    }

    [Fact]
    public async Task LoginWithLdap_TimeoutErrorOccurs_AppropriateMessageAndSuccessCodeReturned()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.ForceTimeoutError, FakeLdapConnectionService.PasswordThatIsAlwaysWrong);
        Assert.Equal(LdapLoginAttemptResultExtensions.TimeoutErrorMessage, reply.Message);
        Assert.False(reply.Success);
    }

    [Fact]
    public async Task LoginWithLdap_UnexpectedErrorOccurs_IsHandledGracefully()
    {
        var exception = await Record.ExceptionAsync(() =>
            LogInWithCredentials(
                FakeLdapConnectionService.ForceUnexpectedError,
                FakeLdapConnectionService.PasswordThatIsAlwaysWrong
            )
        );
        Assert.Null(exception);
    }

    [Fact]
    public async Task LoginWithLdap_UnexpectedErrorOccurs_AppropriateMessageAndSuccessCodeReturned()
    {
        var reply = await LogInWithCredentials(FakeLdapConnectionService.ForceUnexpectedError, FakeLdapConnectionService.PasswordThatIsAlwaysWrong);
        Assert.Equal(LdapLoginAttemptResultExtensions.UnexpectedErrorMessage, reply.Message);
        Assert.False(reply.Success);
    }
}