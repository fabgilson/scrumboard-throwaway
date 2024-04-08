

using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace ScrumBoard.Extensions
{
    public static class AuthenticationStateExtensions
    {
        /// <summary>
        /// Finds the current user id from within this AuthenticationState
        /// </summary>
        /// <returns>Id of the current user or null if not logged in</returns>
        public static long? GetCurrentUserId(this AuthenticationState authState) {
            var nameid = authState.User.FindFirst(JwtRegisteredClaimNames.NameId);
            if (nameid == null) return null;

            return long.Parse(nameid.Value);
        }
    }
}