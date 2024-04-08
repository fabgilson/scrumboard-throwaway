using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using SharedLensResources;

namespace IdentityProvider.Validation;

public class ValidationState
{
    public bool IsValid { get; private set; } = true;

    private ICollection<ValidationResult> ValidationResults { get; } = new List<ValidationResult>();

    /// <summary>
    /// Manually add a new validation result about some property, and sets IsValid to false.
    /// Commonly used for adding validation based on DB integrity, such as duplicate fields that should be unique.
    /// </summary>
    /// <param name="validationResult">New validation result to include in validation state</param>
    public void AddValidationResult(ValidationResult validationResult)
    {
        IsValid = false;
        ValidationResults.Add(validationResult);   
    }

    /// <summary>
    /// Trigger a validation of the values within some entity against the specified validation constraints of that
    /// entity type. Returns a ValidationState response detailing whether the entity is considered 'valid', or if not,
    /// what the exact issues are. 
    /// </summary>
    /// <param name="entity">Entity to validate against the constraints defined for its underlying type.</param>
    /// <typeparam name="T">Type of entity to validate.</typeparam>
    /// <returns>ValidationState describing the outcome of the entity's validation</returns>
    public static ValidationState ForEntity<T>(T entity)
    {
        var state = new ValidationState();
        var validationContext = new ValidationContext(entity);
        state.IsValid = Validator.TryValidateObject(entity, validationContext, state.ValidationResults, true);
        return state;
    }

    public ValidationResponse ToValidationResponse(string message = "")
    {
        return new ValidationResponse
        {
            Message = message,
            IsSuccess = IsValid,
            ValidationErrors =  { ValidationResults.Select(x => 
                new ValidationError {
                    ErrorText = x.ErrorMessage, 
                    FieldNames = { x.MemberNames }
                }).ToList()
            }
        };
    }

    public void WithIdentityErrorsForProperty(IdentityResult identityResult, string propertyName)
    {
        foreach (var validationError in identityResult.Errors)
        {
            AddValidationResult(new ValidationResult(validationError.Description, new[] { propertyName }));
        }
    }
}