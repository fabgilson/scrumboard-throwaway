using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Validator for checking that a DateOnly field represents a date that is either today or in the future
/// </summary>
namespace ScrumBoard.Validators
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class DateInFuture : ValidationAttribute
    {   
        public override bool IsValid(object value) {
            if (value is null) return true;
            if (value is DateOnly date) {
                DateOnly now = DateOnly.FromDateTime(DateTime.Now);
                return date >= now;
            } 
            if (value is DateTime dateTime) {
                return dateTime >= DateTime.Now;
            }
            throw new ArgumentException($"{value.GetType()} cannot be validated using this validator");
        }
    }
}