using EnumsNET;
using FluentAssertions;
using FluentAssertions.Execution;
using IdentityProvider.Models.Entities;
using SharedLensResources;

namespace IdentityProvider.Tests.Integration;

public static class AssertionHelpers
{
    public static void ShouldMatch(this UserResponse userResponse, User user, bool checkId=true)
    {
        using (new AssertionScope())
        {
            if(checkId) userResponse.Id.Should().Be(user.Id);
            userResponse.Email.Should().Be(user.Email);
            userResponse.Created.ToDateTimeOffset().Should().Be(user.Created);
            userResponse.FirstName.Should().Be(user.FirstName);
            userResponse.LastName.Should().Be(user.LastName);
            userResponse.IdentitySource.Should().Be(user.IdentitySource.AsString());
            userResponse.LensRole.Should().Be(user.GlobalLensRole);
            userResponse.UserName.Should().Be(user.UserName);
        }
    }
}