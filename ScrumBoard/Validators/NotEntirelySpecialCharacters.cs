using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

/// <summary>
/// Validator for checking that a String field is not entirely made up of special characters (allows numbers)
/// </summary>
namespace ScrumBoard.Validators
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class NotEntirelySpecialCharacters : RegexDoesNotMatch
    {
        public NotEntirelySpecialCharacters() : base(@"^[^\p{N}\p{L}]+$") {
            ErrorMessage = "Cannot only contain special characters";
        }
    }
}