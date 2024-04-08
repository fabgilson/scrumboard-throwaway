using System;
using System.ComponentModel.DataAnnotations;

namespace ScrumBoard.Validators;

public class DurationValidation
{
     private static string LessThanXErrorMessage(int x, string unit) => $"Must be no less than {x} {unit}";
     private static string GreaterThanXErrorMessage(int x, string unit) => $"Must be no greater than {x} {unit}";

     public static ValidationResult BetweenOneMinuteAndOneDay(TimeSpan duration, ValidationContext context)
     {
          if (duration < TimeSpan.FromMinutes(1))
          {
               return new ValidationResult(LessThanXErrorMessage(1, "minute"), new [] { context.MemberName });
          }
          if (duration > TimeSpan.FromHours(24))
          {
               return new ValidationResult(GreaterThanXErrorMessage(24, "hours"), new[] { context.MemberName });
          }
          return ValidationResult.Success;
     }

     public static ValidationResult BetweenFiveMinutesAndThirtyMinutes(TimeSpan duration, ValidationContext context)
     {
          if (duration < TimeSpan.FromMinutes(5))
          {
               return new ValidationResult(LessThanXErrorMessage(5, "minutes"), new [] { context.MemberName });
          }
          if (duration > TimeSpan.FromMinutes(30))
          {
               return new ValidationResult(GreaterThanXErrorMessage(30, "minutes"), new[] { context.MemberName });
          }
          return ValidationResult.Success;
     }
}