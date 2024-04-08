using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Shared.Modals;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Modals;

public class ManageReviewersModalTest : BaseProjectScopedComponentTestContext<ManageReviewersModal>
{
    private readonly Mock<IProjectMembershipService> _mockProjectMembershipService = new();

    private IEnumerable<IElement> UserAvatarsOnProjectDisplay => ComponentUnderTest.FindAll("[id^=user-avatar-]");

    public ManageReviewersModalTest()
    {
        Services.AddScoped(_ => _mockProjectMembershipService.Object);
        
        // Add dummy ModalTrigger
        ComponentFactories.Add(new ModalTriggerComponentFactory());
    }

    private void CreateComponent(List<Project> availableProjects=null, IEnumerable<User> additionalDevelopersForCurrentProject=null)
    {
        ProjectRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
            .ReturnsAsync(availableProjects ?? [CurrentProject]);
            
        CreateComponentUnderTest(otherDevelopersOnTeam: additionalDevelopersForCurrentProject);
    }

    /// <summary>
    /// Shows the modal with _sprint as sprint to select reviewers for
    /// </summary>
    /// <returns>
    /// Task that will complete when the modal is shown, which contains another task for when the modal returns a value
    /// </returns>
    private async Task<Task<bool>> Show()
    {
        Task<bool> showResultTask = null;
        await ComponentUnderTest.InvokeAsync(() => { showResultTask = ComponentUnderTest.Instance.Show(CurrentProject); });
        return showResultTask;
    }

    [Fact]
    public async Task Show_Called_ModalShown()
    {
        CreateComponent();
        ComponentUnderTest.FindAll(".modal-body").Should().BeEmpty();
        await Show();
        ComponentUnderTest.FindAll(".modal-body").Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(".btn-close")]
    [InlineData("#close-modal")]
    public async Task Showing_ACloseButtonPressed_ModalClosed(string closeButtonSelector)
    {
        CreateComponent();
        var resultTask = await Show();
        ComponentUnderTest.Find(closeButtonSelector).Click();
        (await resultTask).Should().BeTrue();
    }

    [Fact]
    public async Task Showing_NoProjects_NoProjectsAvailableTextShown()
    {
        CreateComponent();
        await Show();
        ComponentUnderTest.Find(".modal-body").TextContent.Should().Contain("No projects available as reviewers");
    }

    [Fact]
    public async Task Showing_AllProjectsHaveCommonUsers_NoProjectsAvailableTextShown()
    {
        var commonUser = FakeDataGenerator.CreateFakeUser();
        var secondProject = FakeDataGenerator.CreateFakeProject(developers: [commonUser]);
        CreateComponent(additionalDevelopersForCurrentProject: [commonUser], availableProjects: [CurrentProject, secondProject]);
        
        await Show();
        
        ComponentUnderTest.Find(".modal-body").TextContent.Should().Contain("No projects available as reviewers");
    }

    [Theory]
    [InlineData(ProjectRole.Leader)]
    [InlineData(ProjectRole.Reviewer)]
    [InlineData(ProjectRole.Guest)]
    public async Task Showing_OtherProjectHasNoNewDevelopers_NoProjectsAvailableTextShown(ProjectRole role)
    {
        var otherProject = FakeDataGenerator.CreateFakeProject(developers: [FakeDataGenerator.CreateFakeUser()]);
        otherProject.MemberAssociations.First().Role = role;
        CreateComponent(availableProjects: [otherProject]);

        await Show();
        
        ComponentUnderTest.Find(".modal-body").TextContent.Should().Contain("No projects available as reviewers");
    }

    [Fact]
    public async Task Showing_OtherProjectsWithMixedRoleUsers_OnlyDeveloperUserAvatarsShown()
    {
        var otherProject = FakeDataGenerator.CreateFakeProject();
        var developer = FakeDataGenerator.CreateFakeUser();
        
        otherProject.MemberAssociations.Add(new ProjectUserMembership { Role = ProjectRole.Guest, User = FakeDataGenerator.CreateFakeUser() });
        otherProject.MemberAssociations.Add(new ProjectUserMembership { Role = ProjectRole.Reviewer, User = FakeDataGenerator.CreateFakeUser() });
        otherProject.MemberAssociations.Add(new ProjectUserMembership { Role = ProjectRole.Developer, User = developer });
        otherProject.MemberAssociations.Add(new ProjectUserMembership { Role = ProjectRole.Leader, User = FakeDataGenerator.CreateFakeUser() });

        CreateComponent(availableProjects: [otherProject]);
        
        await Show();

        UserAvatarsOnProjectDisplay.Should().ContainSingle().Which.Id.Should().Be($"user-avatar-{developer.Id}");
    }

    [Theory]
    [InlineData("first", 1)]
    [InlineData("project", 3)]
    [InlineData("another project", 1)]
    public async Task Showing_ProjectFiltered_MatchingProjectsShown(string filter, int expectedMatches)
    {
        var testUser = FakeDataGenerator.CreateFakeUser();
        CreateComponent(availableProjects: [
            FakeDataGenerator.CreateFakeProject(namePrefix: "First", developers: [testUser], includeUserObjectInMembershipEntities: true),
            FakeDataGenerator.CreateFakeProject(namePrefix: "Second project", developers: [testUser], includeUserObjectInMembershipEntities: true),
            FakeDataGenerator.CreateFakeProject(namePrefix: "Third project", developers: [testUser], includeUserObjectInMembershipEntities: true),
            FakeDataGenerator.CreateFakeProject(namePrefix: "ANOTHER PROJECT", developers: [testUser], includeUserObjectInMembershipEntities: true)
        ]);

        await Show();

        ComponentUnderTest.Find("#project-search").Input(filter);
        ComponentUnderTest.FindAll(".project-list-item").Should().HaveCount(expectedMatches);
    }

    [Fact]
    public async Task Showing_RemoveAllReviewersPressed_AllReviewersAreRemoved()
    {
        CreateComponent();

        CurrentProject.MemberAssociations.Add(new ProjectUserMembership
        {
            User = FakeDataGenerator.CreateFakeUser(),
            Role = ProjectRole.Reviewer
        });

        await Show();

        ComponentUnderTest.FindAll("#user-list-item").Should().HaveCount(1);
        ComponentUnderTest.Find("#remove-reviewers").Click();

        _mockProjectMembershipService.Verify(mock => mock.RemoveAllReviewersFromProject(ActingUser, CurrentProject), Times.Once);
    }

    [Fact]
    public async Task Showing_ProjectSelectedAndClicked_DevelopersAreAddedAsReviewers()
    {
        var testProject = FakeDataGenerator.CreateFakeProject(
            developers: FakeDataGenerator.CreateMultipleFakeUsers(2), 
            includeUserObjectInMembershipEntities: true
        );
        CreateComponent(availableProjects: [testProject]);

        await Show();

        ComponentUnderTest.FindAll(".project-list-item").Should().HaveCount(1);
        ComponentUnderTest.Find(".project-list-item").Click();
        ComponentUnderTest.Find("#confirm-select-reviewers").Click();

        _mockProjectMembershipService.Verify(mock => 
            mock.AddMembersOfProjectAsReviewers(ActingUser, testProject, CurrentProject));
    }
}