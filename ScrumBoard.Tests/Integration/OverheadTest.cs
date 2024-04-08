using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities;
using Xunit;
using System.Collections.Generic;
using System;
using Bunit;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Pages;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Shared;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Tests.Util;
using ScrumBoard.Utils;


namespace ScrumBoard.Tests.Integration;

public class OverheadTest : TestContext
{

    private IDbContextFactory<DatabaseContext> _databaseContextFactory;

    private User _actingUser;
        
    private readonly TimeSpan _currentOverheadDuration = TimeSpan.FromHours(2);
    private readonly TimeSpan _previousOverheadDuration = TimeSpan.FromHours(3);
        
    private readonly string _currentOverheadDescription = "Current sprint overhead";

    private Sprint _currentSprint;
    private Sprint _previousSprint;
    private Sprint _otherProjectSprint;

    private Project _project;
    private Project _otherProject;

    private OverheadSession _session;

    private readonly Mock<IScrumBoardStateStorageService> _mockStateStorageService = new(MockBehavior.Strict);
    private readonly Mock<IJsInteropService> _mockJsInteropService = new();
        
    private IRenderedComponent<Overhead> _component;
        
    public OverheadTest()
    {
        Services.AddDbContextFactory<DatabaseContext>(options =>
            options.UseInMemoryDatabase("OverheadTestInMemDb")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        );
            
        Services.AddScoped<IProjectRepository, ProjectRepository>();
        Services.AddScoped<IOverheadEntryRepository, OverheadEntryRepository>();
        Services.AddScoped<IOverheadEntryChangelogRepository, OverheadEntryChangelogRepository>();
        Services.AddScoped<IOverheadSessionRepository, OverheadSessionRepository>();
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);
        Services.AddScoped(_ => _mockStateStorageService.Object);
        Services.AddScoped(_ => _mockJsInteropService.Object);

        RepopulateDatabase();

        ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
    }

    private void RepopulateDatabase()
    {
        _databaseContextFactory = Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        _actingUser = new User() { FirstName = "Guy", LastName = "Fieri" };

        _session = new OverheadSession()
        {
            Name = "Test session",
        };
            
        _project = new Project()
        {
            Name = "Main project"
        };

        _currentSprint = new Sprint
        { 
            Creator = _actingUser,
            Name = "Current Sprint Name", 
            Stage = SprintStage.Started,
            OverheadEntries = new List<OverheadEntry> {
                new() {
                    Description = _currentOverheadDescription,
                    Session = _session,
                    User = _actingUser,
                    Duration = _currentOverheadDuration,
                }
            }
        };

        _previousSprint = new Sprint
        { 
            Creator = _actingUser,
            Name = "Initial Sprint Name", 
            Stage = SprintStage.Closed,
            OverheadEntries = new List<OverheadEntry> {
                new() {
                    Description = "Previous sprint overhead",
                    Session = _session,
                    User = _actingUser,
                    Duration = _previousOverheadDuration,
                }
            }
        };

        _project.Sprints.Add(_previousSprint);
        _project.Sprints.Add(_currentSprint);
            
        _otherProject = new Project()
        {
            Name = "Other project"
        };
        _otherProjectSprint = new Sprint
        { 
            Creator = _actingUser,
            Name = "Other Project Sprint Name", 
            Stage = SprintStage.Started,
            OverheadEntries = new List<OverheadEntry> {
                new() {
                    Description = "Other project overhead",
                    Session = _session,
                    User = _actingUser,
                    Duration = TimeSpan.FromHours(0.5),
                }
            }
        };
        _otherProject.Sprints.Add(_otherProjectSprint);
            
            

        ProjectUserMembership projectUserMembership = new() { User = _actingUser, Project = _project, Role = ProjectRole.Leader};
        _actingUser.ProjectAssociations.Add(projectUserMembership);
        _project.MemberAssociations.Add(projectUserMembership);

        using var context = _databaseContextFactory.CreateDbContext();
        context.Users.Add(_actingUser);
        context.Projects.AddRange(_project, _otherProject);                     
        context.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        using var context = _databaseContextFactory.CreateDbContext();
        context.OverheadEntries.RemoveRange(context.OverheadEntries);
        context.Sprints.RemoveRange(context.Sprints);
        context.Users.RemoveRange(context.Users);
        context.Projects.RemoveRange(context.Projects);
        context.OverheadSessions.RemoveRange(context.OverheadSessions);
        context.SaveChanges();
            
        base.Dispose(disposing);
    }

    private void CreateComponent()
    {
        _component = RenderComponent<Overhead>(parameters => parameters
            .AddCascadingValue("Self", _actingUser)
            .AddCascadingValue("ProjectState", new ProjectState() {IsReadOnly = false, ProjectId = _project.Id, ProjectRole = ProjectRole.Leader}));
    }

    [Fact]
    public void RenderComponent_ShowCurrentSprint_TotalOverheadTimeIsForCurrentSprint()
    {
        CreateComponent();
        var durationString = DurationUtils.DurationStringFrom(_currentOverheadDuration);
        _component.Find("#total-time-spent").TextContent.Should().Contain(durationString);
    }
        
    [Fact]
    public void RenderComponent_ShowWholeProject_TotalOverheadTimeIsForWholeProject()
    {
        CreateComponent();
        _component.Find("#select-whole-project").Click();
            
        var durationString = DurationUtils.DurationStringFrom(_currentOverheadDuration + _previousOverheadDuration);
        _component.Find("#total-time-spent").TextContent.Should().Contain(durationString);
    }
        
    [Fact]
    public void RenderComponent_ShowCurrentSprint_OverheadForSprintShown()
    {
        CreateComponent();
        var overhead = _component.FindAll(".overhead-entry").Should().ContainSingle().Which;
        overhead.TextContent.Should().Contain(_currentOverheadDescription);
        overhead.TextContent.Should().Contain(DurationUtils.DurationStringFrom(_currentOverheadDuration));
    }
        
    [Fact]
    public void RenderComponent_ShowWholeProject_OverheadForProjectShown()
    {
        CreateComponent();
        _component.Find("#select-whole-project").Click();
        _component.FindAll(".overhead-entry").Should().HaveCount(2);
    }
}