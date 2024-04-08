using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using ScrumBoard.Extensions;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace ScrumBoard.Tests.Unit.Extensions
{
    public class AuthenticationStateExtensionsTest
    {
        
        [Fact]
        public void GetCurrentUserId_NoClaim_NullReturned()
        {
            var authState = new AuthenticationState(new ClaimsPrincipal());
            authState.GetCurrentUserId().Should().BeNull();
        }

        [Fact]
        public void GetCurrentUserId_HasClaimWithUserId_UserIdReturned()
        {
            long userId = 42;

            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim("foo", "bar"));
            identity.AddClaim(new Claim(JwtRegisteredClaimNames.NameId, userId.ToString()));

            var authState = new AuthenticationState(new ClaimsPrincipal(identity));
            authState.GetCurrentUserId().Should().Be(userId);
        }
    }
}
