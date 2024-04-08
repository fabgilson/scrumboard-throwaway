using System;
using System.Linq;
using System.Security.Claims;

namespace SharedLensResources.Extensions;

public static class ClaimsExtensions
{
    public static ClaimDto ToDto(this Claim claim)
    {
        return new ClaimDto()
        {
            Issuer = claim.Issuer,
            OriginalIssuer = claim.OriginalIssuer,
            Type = claim.Type,
            Value = claim.Value ?? "",
            ValueType = claim.ValueType
        };
    }

    public static Claim FromDto(this ClaimDto claimDto)
    {
        return new Claim(claimDto.Type, claimDto.Value, claimDto.ValueType, claimDto.Issuer, claimDto.OriginalIssuer);
    }

    public static ClaimsIdentityDto ToDto(this ClaimsIdentity ci)
    {
        ClaimsIdentityDto ciDto = new()
        {
            Name = ci.Name,
            AuthenticationType = ci.AuthenticationType,
            IsAuthenticated = ci.IsAuthenticated,
            Label = ci.Label ?? "",
            NameClaimType = ci.NameClaimType,
            RoleClaimType = ci.RoleClaimType
        };
        if (ciDto == null) throw new ArgumentNullException(nameof(ciDto));
        foreach (var claim in ci.Claims)
        {
            ciDto.Claims.Add(claim.ToDto());
        }
        return ciDto;
    }

    public static ClaimsIdentity FromDto(this ClaimsIdentityDto ciDto)
    {
        var ci = new ClaimsIdentity(ciDto.Claims.Select(x => x.FromDto()), ciDto.AuthenticationType, ciDto.NameClaimType, ciDto.RoleClaimType);
        return ci;
    }
}