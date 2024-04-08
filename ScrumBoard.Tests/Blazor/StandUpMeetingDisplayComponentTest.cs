using System;
using System.Collections.Generic;
using System.Linq;
using ScrumBoard.Shared.StandUpMeetings;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Relationships;
using Xunit;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Shared;

namespace ScrumBoard.Tests.Blazor
{
    public class StandUpMeetingDisplayComponentTest : TestContext
    {
        private IRenderedComponent<StandUpMeetingDisplay> _standUpMeetingDisplay;

        private readonly Mock<IJsInteropService> _jsInteropServiceMock = new();
        private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
        private readonly Mock<IStandUpMeetingService> _standUpMeetingServiceMock = new();

        private static readonly ICollection<User> ProjectDevelopers = new User[]
        {
            new() { Id = 50, FirstName = "Timmy", LastName = "Tester"},
            new() { Id = 51, FirstName = "Tiffany", LastName = "Tester"},
        }; 
        
        private static readonly Project Project = new()
        {
            Name = "Team X",
            Id = 789,
            MemberAssociations = ProjectDevelopers.Select(x => new ProjectUserMembership
            {
                User = x,
                Role = ProjectRole.Developer
            }).ToArray()
        };

        private static StandUpMeeting StandUpWithStartDate(
            DateTime scheduledStart, 
            string name="Team X stand-up",
            string notes="", 
            string location=""
        ) => new() {
            Name = name,
            ScheduledStart = scheduledStart,
            Location = location,
            Notes = notes,
            ExpectedAttendances = ProjectDevelopers.Select(x => new StandUpMeetingAttendance { User = x }).ToArray()
        };
        
        public StandUpMeetingDisplayComponentTest()
        {
            _projectRepositoryMock.Setup(x => x.GetByStandUpMeetingAsync(It.IsAny<StandUpMeeting>())).ReturnsAsync(Project);
            _projectRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<long>())).ReturnsAsync(Project);
            
            Services.AddScoped(_ => _projectRepositoryMock.Object);
            Services.AddScoped(_ => _standUpMeetingServiceMock.Object);
            Services.AddScoped(_ => _jsInteropServiceMock.Object);
        }
        
        private void CreateComponent(
            StandUpMeeting standUpMeeting, 
            bool showProjectName=false, 
            bool showLocation=false, 
            bool showNotes=false
        ) {
            _standUpMeetingDisplay = RenderComponent<StandUpMeetingDisplay>(parameters => parameters
                .AddCascadingValue("Self", ProjectDevelopers.First())
                .AddCascadingValue("ProjectState", new ProjectState { ProjectId = Project.Id, ProjectRole = ProjectRole.Developer, Project = Project})
                .Add(p => p.StandUpMeeting, standUpMeeting)
                .Add(p => p.ShowLocationSection, showLocation)
                .Add(p => p.ShowNotesSection, showNotes)
                .Add(p => p.ShowProjectNameSection, showProjectName)
            );
        }

        [Fact]
        private void Rendered_StandUpHasSpecifiedScheduledStart_TimeAndDateShown()
        {
            // The datetime we use here is fixed, or else we would just be checking that calling
            // DateTime.ToString(format) the same way twice returns the same values each time.
            var standUp = StandUpWithStartDate(new DateTime(2022, 5, 4, 16, 30, 5));
            CreateComponent(standUp);
            _standUpMeetingDisplay
                .Find("#time-and-date-display").TextContent
                .Should().BeEquivalentTo("Wed 04 May, 4:30 pm");
        }
        
        public static readonly TheoryData<DateTime, string> FutureStandUpTheoryData = new()
        {
            {DateTime.Now.AddDays(1).AddHours(1).AddSeconds(30), "Starts in 1 day, 1 hour from now."},
            {DateTime.Now.AddHours(2).AddMinutes(5).AddSeconds(30), "Starts in 2 hours, 5 minutes from now."},
            {DateTime.Now.AddMinutes(32).AddSeconds(30), "Starts in 32 minutes from now."},
            {DateTime.Now.AddDays(-1).AddHours(-1).AddSeconds(-30), "Scheduled start was 1 day, 1 hour ago."},
            {DateTime.Now.AddHours(-2).AddMinutes(-5).AddSeconds(-30), "Scheduled start was 2 hours, 5 minutes ago."},
            {DateTime.Now.AddMinutes(-32).AddSeconds(-30), "Scheduled start was 32 minutes ago."},
        };

        [Theory]
        [MemberData(nameof(FutureStandUpTheoryData))]
        private void Rendered_StandUpHasSpecifiedScheduledStartInFuture_CorrectTimeUntilStartShown(DateTime start, string expectedLabel)
        {
            // The datetime we use here is fixed, or else we would just be checking that calling
            // DateTime.ToString(format) the same way twice returns the same values each time.
            var standUp = StandUpWithStartDate(start);
            CreateComponent(standUp);
            _standUpMeetingDisplay
                .Find("#time-from-now-display").TextContent
                .Should().BeEquivalentTo(expectedLabel);
        }

        [Fact]
        private void Rendered_ShowProjectNameSectionEnabled_ProjectNameIsShown()
        {
            var standUp = StandUpWithStartDate(DateTime.Now.AddHours(1));
            CreateComponent(standUp, showProjectName: true);
            _standUpMeetingDisplay
                .Find("#project-name-display").TextContent
                .Should().BeEquivalentTo(Project.Name);
        }

        [Fact]
        private void Rendered_ShowProjectNameSectionDisabled_ProjectNameNotShown()
        {
            var standUp = StandUpWithStartDate(DateTime.Now.AddHours(1));
            CreateComponent(standUp, showProjectName: false);
            Action act = () => _standUpMeetingDisplay.Find("#project-name-display");
            act.Should().Throw<ElementNotFoundException>();
        }
        
        [Fact]
        private void Rendered_ShowNotesSectionEnabledButNoNotesGiven_NotesSectionShownButWithDefaultMessage()
        {
            var standUp = StandUpWithStartDate(DateTime.Now.AddHours(1), notes: "");
            CreateComponent(standUp, showNotes: true);
            _standUpMeetingDisplay
                .Find("#stand-up-notes-content").TextContent
                .Should().BeEquivalentTo("No notes found");
        }
        
        [Fact]
        private void Rendered_ShowNotesSectionEnabledWithNotesGiven_NotesShown()
        {
            var standUp = StandUpWithStartDate(DateTime.Now.AddHours(1), notes: "Our meeting notes");
            CreateComponent(standUp, showNotes: true);
            _standUpMeetingDisplay
                .Find("#stand-up-notes-content").TextContent.Trim()
                .Should().BeEquivalentTo("Our meeting notes");
        }
        
        [Fact]
        private void Rendered_ShowNotesSectionDisabled_NotesNotShown()
        {
            var standUp = StandUpWithStartDate(DateTime.Now.AddHours(1));
            CreateComponent(standUp, showNotes: false);
            Action act = () => _standUpMeetingDisplay.Find("#stand-up-notes-content");
            act.Should().Throw<ElementNotFoundException>();
        }
        
        [Fact]
        private void Rendered_ShowLocationSectionEnabledButNoLocationGiven_LocationSectionShownButWithDefaultMessage()
        {
            var standUp = StandUpWithStartDate(DateTime.Now.AddHours(1), location: "");
            CreateComponent(standUp, showLocation: true);
            _standUpMeetingDisplay
                .Find("#stand-up-location-content").TextContent
                .Should().BeEquivalentTo("No location set");
        }
        
        [Fact]
        private void Rendered_ShowLocationSectionEnabledWithLocationGiven_LocationShown()
        {
            var standUp = StandUpWithStartDate(DateTime.Now.AddHours(1), location: "Our meeting location");
            CreateComponent(standUp, showLocation: true);
            _standUpMeetingDisplay
                .Find("#stand-up-location-content").TextContent.Trim()
                .Should().BeEquivalentTo("Our meeting location");
        }
        
        [Fact]
        private void Rendered_ShowLocationSectionDisabled_LocationNotShown()
        {
            var standUp = StandUpWithStartDate(DateTime.Now.AddHours(1));
            CreateComponent(standUp, showLocation: false);
            Action act = () => _standUpMeetingDisplay.Find("#stand-up-location-content");
            act.Should().Throw<ElementNotFoundException>();
        }
    }
}