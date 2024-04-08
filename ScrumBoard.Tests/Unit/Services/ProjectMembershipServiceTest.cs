using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using Xunit;

namespace ScrumBoard.Tests.Unit.Services;

public class ProjectMembershipServiceTest
{
    private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);
    private readonly Mock<IProjectChangelogRepository> _mockProjectChangelogRepository = new(MockBehavior.Strict);
    private readonly IProjectMembershipService _service;
    private readonly Project _project;
    private readonly User _actingUser;

    public ProjectMembershipServiceTest()
    {
        _mockProjectRepository
            .Setup(mock => mock.UpdateMemberships(It.IsAny<Project>()))
            .Returns(Task.CompletedTask);

        _mockProjectChangelogRepository
            .Setup(mock => mock.AddAllAsync(It.IsAny<List<ProjectUserMembershipChangelogEntry>>()))
            .Returns(Task.CompletedTask);

        _service = new ProjectMembershipService(_mockProjectRepository.Object, _mockProjectChangelogRepository.Object);
        _actingUser = new User
        {
            Id = 20,
            FirstName = "Jimmy",
            LastName = "Neutron",
        };
        _project = new Project
        {
            Id = 14,
            Name = "Test project",
        };
        _project.MemberAssociations.Add(new ProjectUserMembership
        {
            User = _actingUser,
            Role = ProjectRole.Developer
        });
    }

    [Fact]
    public async Task RemoveAllReviewersFromProject_NoReviewers_NoMembershipsChanged()
    {
        await _service.RemoveAllReviewersFromProject(_actingUser, _project);

        _mockProjectRepository.Verify(mock => mock.UpdateMemberships(_project), Times.Once);
        // Checks that no membership changes were made
        _mockProjectChangelogRepository.Verify(
            mock => mock.AddAllAsync(It.Is<List<ProjectUserMembershipChangelogEntry>>(l => l.Count == 0)), Times.Once);
    }

    [Fact]
    public async Task RemoveAllReviewersFromProject_OneReviewer_MembershipsChanged()
    {
        User testUser = new User
        {
            Id = 20,
            FirstName = "Jimmy",
            LastName = "Neutron",
        };
        _project.MemberAssociations.Add(new ProjectUserMembership
        {
            User = testUser,
            Role = ProjectRole.Reviewer
        });

        await _service.RemoveAllReviewersFromProject(_actingUser, _project);

        _mockProjectRepository.Verify(mock => mock.UpdateMemberships(_project), Times.Once);
        // Checks that only one membership change was made
        _mockProjectChangelogRepository.Verify(
            mock => mock.AddAllAsync(It.Is<List<ProjectUserMembershipChangelogEntry>>(l => l.Count == 1)), Times.Once);
    }
}