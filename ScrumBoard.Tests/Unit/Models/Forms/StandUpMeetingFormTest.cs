using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using Xunit;

namespace ScrumBoard.Tests.Unit.Models.Forms;

public class StandUpMeetingFormTest
{
    private static readonly StandUpMeetingForm ValidStandUpMeeting = new()
    {
        Name = "Valid Stand Up Meeting", 
        Location = "", 
        Notes = "", 
        Duration = TimeSpan.FromMinutes(10)
    };

    private static List<ValidationResult> ValidateModel(StandUpMeetingForm model) {
        List<ValidationResult> results = new();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }

    [Fact]
    public void ArrangeValidStandUp_SprintHasEnded_IsFailure()
    {
        var standUpMeeting = ValidStandUpMeeting;
        standUpMeeting.Sprint = new Sprint { EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)) };
        standUpMeeting.ScheduledStart = DateTime.Now.AddHours(1);
        var result = ValidateModel(standUpMeeting);
        result.Should().HaveCount(1);
        result.First().ErrorMessage.Should().StartWith("Scheduled start cannot occur after the sprint has ended");
    }

    //[Fact]
    //public void ArrangeValidStandUp_SprintNotEnded_IsSuccess()
    //{
    //  var standUpMeeting = ValidStandUpMeeting;
    //    standUpMeeting.Sprint = new Sprint { EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)) };
    //    standUpMeeting.ScheduledStart = DateTime.Now.AddHours(1);
    //    var result = ValidateModel(standUpMeeting);
    //    result.Should().BeEmpty();
    //}
}
