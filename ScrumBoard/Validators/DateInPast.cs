using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Validator for checking that a DateOnly field represents a date that is in the past
/// </summary>
namespace ScrumBoard.Validators
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class DateInPast : ValidationAttribute
    {   

        public override bool IsValid(object value) {
            if (value is DateOnly date) {
                DateOnly now = DateOnly.FromDateTime(DateTime.Now);
                return date <= now;
            } 
            throw new ArgumentException($"{value.GetType()} cannot be validated using this validator");
        }
    }
}