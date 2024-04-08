using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

/// <summary>
/// Validator for checking that a field does not equal the provided value
/// </summary>
namespace ScrumBoard.Validators
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
    public class NotEquals : ValidationAttribute
    {

        private readonly object _value;

        public NotEquals(object value) {
            _value = value;
        }

        public override bool IsValid(object value) {
            return !Object.Equals(value, _value);
        }
    }
}