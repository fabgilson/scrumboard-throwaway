using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

/// <summary>
/// Validator for checking that a String field is does not match the provided regular expression
/// </summary>
namespace ScrumBoard.Validators
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class RegexDoesNotMatch : ValidationAttribute
    {
        private String Pattern { get; }

        public RegexDoesNotMatch(string pattern) {
            Pattern = pattern;
        }

        public override bool IsValid(object value) {
            if (value == null) value = "";
            if (value is string s) {
                return !Regex.IsMatch(s, Pattern);
            }
            throw new ArgumentException($"{value.GetType()} cannot be validated using this validator");
        }
    }
}