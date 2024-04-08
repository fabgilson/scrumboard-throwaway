using System;
using System.Security.Claims;
using System.Text;
using IdentityProvider.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace IdentityProvider.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var key = Encoding.ASCII.GetBytes(configuration.GetValue<string>("SigningKey"));

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie(IdentityConstants.ApplicationScheme)    
            .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                    x.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            await using var dbContext = context.HttpContext.RequestServices.GetRequiredService<DatabaseContext>();
                            
                            // Check that the user still exists in the IdP
                            var username = context.Principal.FindFirstValue("unique_name");
                            var dbUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username);
                            if (dbUser is null) { context.Fail("No such user exists for the given token."); }
                        }
                    };
                    // need this to avoid the legacy MS claim types. Sigh*
                    x.TokenValidationParameters.RoleClaimType = "role";
                    x.TokenValidationParameters.NameClaimType = "name";
                });

            services.AddAuthorization(options =>
            {
                // require all users to be authenticated by default
                options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();

                // Register our age limit policy
                options.AddPolicy("Over18YearsOld", policy => policy.RequireAssertion(context =>
                    context.User.HasClaim(c =>
                        (c.Type == "DateOfBirth" && DateTime.Now.Year - DateTime.Parse(c.Value).Year >= 18)
                    )));
            });

            return services;
        }
    }
}