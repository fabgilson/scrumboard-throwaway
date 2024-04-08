using System;

namespace ScrumBoard.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NotEntirelyNumbersOrSpecialCharactersAttribute : RegexDoesNotMatch
{
    public NotEntirelyNumbersOrSpecialCharactersAttribute() : base(@"^[\P{L}]+$") {
        ErrorMessage = "Cannot only contain numbers or special characters";
    }
}