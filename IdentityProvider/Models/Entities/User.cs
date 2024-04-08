using System;
using System.ComponentModel.DataAnnotations;
using EnumsNET;
using Google.Protobuf.WellKnownTypes;
using IdentityProvider.Validation;
using Microsoft.AspNetCore.Identity;
using SharedLensResources;

namespace IdentityProvider.Models.Entities;

public enum IdentitySource
{
    Ldap,
    Lens,
}

public class User : IdentityUser<long>
{
    [MaxLength(64)]
    [Required]
    [PersonalData]
    public string FirstName { get; set; }

    [MaxLength(64)]
    [Required]
    [PersonalData]
    public string LastName { get; set; }

    [PersonalData]
    public DateTime Created { get; set; }

    [Required]
    [MaxLength(64)]
    public override string UserName { get; set; }
    
    [MaxLength(64)]
    public override string NormalizedUserName { get; set; }

    [Required]
    public GlobalLensRole GlobalLensRole { get; set; }
    
    public IdentitySource IdentitySource { get; set; }

    [Required]
    [EmailAddress]
    public override string Email { get; set; }

    /// <summary>
    /// The organisation's internal ID for some user, e.g a UC student ID '12345678'
    /// </summary>
    [PersonalData]
    public int? EmployeeId { get; set; }
}

public static class UserExtensions
{
    public static User ToEntity(this CreateNewLensAccountRequest request, out ValidationState validationState)
    {
        var user = new User
        {
            Created = DateTime.Now,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            GlobalLensRole = request.Role,
            IdentitySource = IdentitySource.Lens
        };
        validationState = ValidationState.ForEntity(user);
        return user;
    }
    
    public static User ToModifiedEntity(this UpdateUserRequest request, User existingUserValue, out ValidationState validationState)
    {
        existingUserValue.Created = DateTime.Now;
        existingUserValue.Email = request.NewEmail ?? existingUserValue.Email;
        existingUserValue.FirstName = request.NewFirstName ?? existingUserValue.FirstName;
        existingUserValue.LastName = request.NewLastName ?? existingUserValue.LastName;
        existingUserValue.UserName = request.NewUserName ?? existingUserValue.UserName;
        existingUserValue.GlobalLensRole = request.HasNewRole ? request.NewRole : existingUserValue.GlobalLensRole;

        validationState = ValidationState.ForEntity(existingUserValue);
        
        if (existingUserValue.IdentitySource is not IdentitySource.Lens && request.NewUserName is not null)
        {
            validationState.AddValidationResult(new ValidationResult(
                "User account (and therefore username) is not managed by LENS",
                new[] { nameof(request.NewUserName) }));
        }
        
        return existingUserValue;
    }

    public static UserResponse ToUserResponse(this User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            Created = Timestamp.FromDateTimeOffset(user.Created),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IdentitySource = user.IdentitySource.AsString(),
            LensRole = user.GlobalLensRole
        };
    }
}