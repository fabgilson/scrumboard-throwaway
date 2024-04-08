using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Shared.UsageData;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor;

public class EditProjectComponentTest : TestContext
{
    private IRenderedComponent<ProjectEditView> _component;

    private readonly Mock<IUserRepository> _mockUserRepository = new(MockBehavior.Strict);

    private readonly Mock<IProjectRepository> _mockProjectRepository = new(MockBehavior.Strict);

    private readonly Mock<IGitlabService> _mockGitlabService = new(MockBehavior.Strict);

    private readonly Mock<IProjectChangelogRepository> _mockProjectChangelogRepository = new(MockBehavior.Strict);

    private readonly Mock<IConfigurationService> _mockConfigurationService = new(MockBehavior.Strict);

    private readonly Mock<IJsInteropService> _mockJsInteropService = new(MockBehavior.Strict);

    private readonly User _actingUser;

    private readonly User _anotherUser;

    private readonly Project _project;

    public EditProjectComponentTest()
    {
        _actingUser = new User {Id = 1, FirstName = "Jeff", LastName = "Jefferson" };
        _anotherUser = new User {Id = 2, FirstName = "Bob", LastName = "Boberson" };

        _project = new Project
        {
            Id = 1, 
            Name = "MyProject", 
            Description = "This is a cool project", 
            StartDate = new DateOnly(2021, 11, 15), 
            EndDate = new DateOnly(2022, 3, 18), 
            Created = DateTime.Now
        };

        ProjectUserMembership association = new ProjectUserMembership
        { 
            UserId = _actingUser.Id, 
            ProjectId = _project.Id, 
            User = _actingUser, 
            Project = _project, 
            Role = ProjectRole.Leader
        };

        ProjectUserMembership anotherAssociation = new ProjectUserMembership
        { 
            UserId = _anotherUser.Id, 
            ProjectId = _project.Id, 
            User = _anotherUser, 
            Project = _project, 
            Role = ProjectRole.Developer
        };
            
        _project.MemberAssociations.Add(association);
        _project.MemberAssociations.Add(anotherAssociation);
        _actingUser.ProjectAssociations.Add(association);
        _anotherUser.ProjectAssociations.Add(anotherAssociation);
            
        _mockJsInteropService
            .Setup(mock => mock.GetDocumentUrl())
            .ReturnsAsync("localhost");
        _mockUserRepository
            .Setup(x => x.GetByIdAsync(_actingUser.Id))
            .ReturnsAsync(_actingUser);
            
        Services.AddScoped(_ => _mockConfigurationService.Object);
        Services.AddScoped(_ => _mockUserRepository.Object);
        Services.AddScoped(_ => _mockProjectRepository.Object);
        Services.AddScoped(_ => _mockProjectChangelogRepository.Object);
        Services.AddScoped(_ => _mockGitlabService.Object);
        Services.AddScoped(_ => _mockJsInteropService.Object);
        Services.AddScoped(_ => new Mock<IEntityLiveUpdateService>().Object);
            
        ComponentFactories.AddDummyFactoryFor<Markdown>();
        ComponentFactories.AddDummyFactoryFor<ProjectViewLoaded>();
    }

    private void CreateComponent(bool webhooksEnabled) 
    {
        _mockConfigurationService.Setup(x => x.WebhooksEnabled).Returns(webhooksEnabled);
        _mockProjectRepository
            .Setup(mock =>
                mock.GetByIdAsync(It.IsAny<long>(), It.IsAny<Func<IQueryable<Project>, IQueryable<Project>>[]>()))
            .ReturnsAsync(_project);
        _component = RenderComponent<ProjectEditView>(parameters => parameters
            .Add(editProject => editProject.Self, _actingUser)
            .Add(editProject => editProject.Project, _project)  
            .AddCascadingValue("ProjectState", new ProjectState{ProjectRole = ProjectRole.Leader})
        );
    }

    private void SetupSave()
    {
        _mockProjectRepository
            .Setup(mock =>
                mock.UpdateProjectAndMemberships(It.IsAny<Project>(), It.IsAny<List<ProjectUserMembership>>()))
            .Returns(Task.CompletedTask);
        _mockProjectChangelogRepository
            .Setup(mock => mock.AddAllAsync(It.IsAny<IEnumerable<ProjectChangelogEntry>>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]       
    public void SaveChanges_WithValidFields_ProjectUpdated() {
        CreateComponent(false);
        var name = "Test Project Name";
        var description = "Test Project Description";
        var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
        var endDate = startDate.AddDays(42);

        _component.Find("#name-input").Change(name);
        _component.Find("#description-input").Change(description);
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));

        SetupSave();
            
        _component.Find("#edit-project-form").Submit(); 


        var arg = new ArgumentCaptor<Project>();
        _mockProjectRepository.Verify(mock => mock.UpdateProjectAndMemberships(arg.Capture(), It.IsAny<List<ProjectUserMembership>>()), Times.Once());
        var updatedProject = arg.Value;
        updatedProject.Name.Should().Be(name);
        updatedProject.Description.Should().Be(description);
        updatedProject.StartDate.Should().Be(startDate);
        updatedProject.EndDate.Should().Be(endDate);
    }

    [Fact]       
    public void SaveChanges_WithUncheckedGitlabFields_CredentialsChecked() {
        CreateComponent(false);
        var name = "Test Project Name";
        var description = "Test Project Description";
        var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
        var endDate = startDate.AddDays(42);
        var gitlabUrl = new Uri("http://example.com");
        var gitlabAccessToken = "AABB";
        var gitlabProjectId = 42;     

        _component.Find("#gitlab-enabled-checkbox").Change(true);

        _component.Find("#name-input").Change(name);
        _component.Find("#description-input").Change(description);
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));
        _component.Find("#project-url-input").Change(gitlabUrl);
        _component.Find("#project-access-token-input").Change(gitlabAccessToken);        
        _component.Find("#project-id-input").Change(gitlabProjectId);

        _mockGitlabService
            .Setup(mock => mock.TestCredentials(It.IsAny<GitlabCredentials>()))
            .Returns(Task.CompletedTask);
            
        SetupSave();

        _component.Find("#edit-project-form").Submit(); 

        _mockGitlabService.Verify(mock => mock.TestCredentials(new GitlabCredentials(gitlabUrl, gitlabProjectId, gitlabAccessToken, null)), Times.Once());
    }

    [Fact]       
    public async Task SaveChanges_WithInvalidGitlabFields_ProjectNotUpdated() {
        CreateComponent(false);
        var name = "Test Project Name";
        var description = "Test Project Description";
        var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
        var endDate = startDate.AddDays(42);
        var gitlabUrl = new Uri("http://example.com");
        var gitlabAccessToken = "AABB";
        var gitlabProjectId = 42;            
        _mockGitlabService.Setup(mock => mock.TestCredentials(It.IsAny<GitlabCredentials>()))
            .ThrowsAsync(new GitlabRequestFailedException(RequestFailure.NotFound));

        _component.Find("#gitlab-enabled-checkbox").Change(true);

        _component.Find("#name-input").Change(name);
        _component.Find("#description-input").Change(description);
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));
        _component.Find("#project-url-input").Change(gitlabUrl);
        _component.Find("#project-access-token-input").Change(gitlabAccessToken);         
        _component.Find("#project-id-input").Change(gitlabProjectId);

        await _component.InvokeAsync(() => _component.Instance.CheckGitlab());

        await _component.Find("#edit-project-form").SubmitAsync(); 

        var arg = new ArgumentCaptor<Project>();
        var secondArg = new ArgumentCaptor<List<ProjectUserMembership>>();
        _mockProjectRepository.Verify(mock => mock.UpdateProjectAndMemberships(arg.Capture(), secondArg.Capture()), Times.Never());  
        _mockGitlabService.Verify(mock => mock.TestCredentials(new GitlabCredentials(gitlabUrl, gitlabProjectId, gitlabAccessToken, null)), Times.Once());
    }

    [Fact]       
    public async Task SaveChanges_WithCheckedGitlabFields_ProjectUpdated() {
        CreateComponent(true);
        var name = "Test Project Name";
        var description = "Test Project Description";
        var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
        var endDate = startDate.AddDays(42);
        var gitlabUrl = new Uri("http://example.com");
        var gitlabAccessToken = "AABB";
        var gitlabProjectId = 42;
        var gitlabPushWebhookSecretToken = "abc";
            
        _mockGitlabService.Setup(mock => mock.TestCredentials(It.IsAny<GitlabCredentials>()))
            .Returns(Task.CompletedTask);

        _component.Find("#gitlab-enabled-checkbox").Change(true);

        _component.Find("#name-input").Change(name);
        _component.Find("#description-input").Change(description);
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));
        _component.Find("#project-url-input").Change(gitlabUrl);
        _component.Find("#project-access-token-input").Change(gitlabAccessToken);
        _component.Find("#project-webhook-secret-token-input").Change(gitlabPushWebhookSecretToken);
        _component.Find("#project-id-input").Change(gitlabProjectId);

        await _component.InvokeAsync(() => _component.Instance.CheckGitlab());

        SetupSave();
            
        await _component.Find("#edit-project-form").SubmitAsync();


        var arg = new ArgumentCaptor<Project>();
        _mockProjectRepository.Verify(mock => mock.UpdateProjectAndMemberships(arg.Capture(), It.IsAny<List<ProjectUserMembership>>()), Times.Once());
        var updatedProject = arg.Value;
        updatedProject.Name.Should().Be(name);
        updatedProject.Description.Should().Be(description);
        updatedProject.StartDate.Should().Be(startDate);
        updatedProject.EndDate.Should().Be(endDate);
        updatedProject.GitlabCredentials.AccessToken.Should().Be(gitlabAccessToken);
        updatedProject.GitlabCredentials.Id.Should().Be(gitlabProjectId);
        updatedProject.GitlabCredentials.GitlabURL.Should().Be(gitlabUrl);
        updatedProject.GitlabCredentials.PushWebhookSecretToken.Should().Be(gitlabPushWebhookSecretToken);

        _mockGitlabService.Verify(mock => mock.TestCredentials(updatedProject.GitlabCredentials), Times.Once());
    }
        
        
    [Fact]       
    public async Task SaveChanges_WithUntrimmedGitlabUrl_GitlabUrlTrimmed() {
        CreateComponent(true);
        var name = "Test Project Name";
        var description = "Test Project Description";
        var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
        var endDate = startDate.AddDays(42);
        var gitlabUrl = new Uri("http://example.com/test");
        var gitlabUrlTrimmed = new Uri("http://example.com");
        var gitlabAccessToken = "AABB";
        var gitlabProjectId = 42;
        var gitlabPushWebhookSecretToken = "abc";
            
        _mockGitlabService
            .Setup(mock => mock.TestCredentials(It.IsAny<GitlabCredentials>()))
            .Returns(Task.CompletedTask);

        _component.Find("#gitlab-enabled-checkbox").Change(true);

        _component.Find("#name-input").Change(name);
        _component.Find("#description-input").Change(description);
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));
        _component.Find("#project-url-input").Change(gitlabUrl);
        _component.Find("#project-access-token-input").Change(gitlabAccessToken);
        _component.Find("#project-webhook-secret-token-input").Change(gitlabPushWebhookSecretToken);
        _component.Find("#project-id-input").Change(gitlabProjectId);

        await _component.InvokeAsync(() => _component.Instance.CheckGitlab());

        SetupSave();
            
        await _component.Find("#edit-project-form").SubmitAsync();


        var arg = new ArgumentCaptor<Project>();
        _mockProjectRepository.Verify(mock => mock.UpdateProjectAndMemberships(arg.Capture(), It.IsAny<List<ProjectUserMembership>>()), Times.Once());
        var updatedProject = arg.Value;
        updatedProject.GitlabCredentials.GitlabURL.Should().Be(gitlabUrlTrimmed);

        _mockGitlabService.Verify(mock => mock.TestCredentials(updatedProject.GitlabCredentials), Times.Once());
    }

    [Fact]       
    public async Task SaveChanges_WithNoPushWebhookSecretToken_WebhooksDisabled_ProjectUpdated() {
        CreateComponent(false);
        var name = "Test Project Name";
        var description = "Test Project Description";
        var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
        var endDate = startDate.AddDays(42);
        var gitlabUrl = new Uri("http://example.com");
        var gitlabAccessToken = "AABB";
        var gitlabProjectId = 42;
        _mockGitlabService.Setup(mock => mock.TestCredentials(It.IsAny<GitlabCredentials>()))
            .Returns(Task.CompletedTask);

        _component.Find("#gitlab-enabled-checkbox").Change(true);

        _component.Find("#name-input").Change(name);
        _component.Find("#description-input").Change(description);
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));
        _component.Find("#project-url-input").Change(gitlabUrl);
        _component.Find("#project-access-token-input").Change(gitlabAccessToken);
        _component.Find("#project-id-input").Change(gitlabProjectId);

        await _component.InvokeAsync(() => _component.Instance.CheckGitlab());

        SetupSave();
            
        await _component.Find("#edit-project-form").SubmitAsync();


        var arg = new ArgumentCaptor<Project>();
        _mockProjectRepository.Verify(mock => mock.UpdateProjectAndMemberships(arg.Capture(), It.IsAny<List<ProjectUserMembership>>()), Times.Once());
        var updatedProject = arg.Value;
        updatedProject.Name.Should().Be(name);
        updatedProject.Description.Should().Be(description);
        updatedProject.StartDate.Should().Be(startDate);
        updatedProject.EndDate.Should().Be(endDate);
        updatedProject.GitlabCredentials.AccessToken.Should().Be(gitlabAccessToken);
        updatedProject.GitlabCredentials.Id.Should().Be(gitlabProjectId);
        updatedProject.GitlabCredentials.GitlabURL.Should().Be(gitlabUrl);
        updatedProject.GitlabCredentials.PushWebhookSecretToken.Should().BeNull();

        _mockGitlabService.Verify(mock => mock.TestCredentials(updatedProject.GitlabCredentials), Times.Once());
    }

    [Fact]       
    public async Task SaveChanges_WithNoPushWebhookSecretToken_WebhooksEnabled_ProjectNotUpdated() {
        CreateComponent(true);
        var name = "Test Project Name";
        var description = "Test Project Description";
        var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
        var endDate = startDate.AddDays(42);
        var gitlabUrl = new Uri("http://example.com/test");
        var gitlabAccessToken = "AABB";
        var gitlabProjectId = 42;
        _mockGitlabService.Setup(mock => mock.TestCredentials(It.IsAny<GitlabCredentials>()))
            .Returns(Task.CompletedTask);

        _component.Find("#gitlab-enabled-checkbox").Change(true);

        _component.Find("#name-input").Change(name);
        _component.Find("#description-input").Change(description);
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));
        _component.Find("#project-url-input").Change(gitlabUrl);
        _component.Find("#project-access-token-input").Change(gitlabAccessToken);
        _component.Find("#project-id-input").Change(gitlabProjectId);

        await _component.InvokeAsync(() => _component.Instance.CheckGitlab());

        SetupSave();
            
        await _component.Find("#edit-project-form").SubmitAsync();


        var arg = new ArgumentCaptor<Project>();
        var secondArg = new ArgumentCaptor<List<ProjectUserMembership>>();
        _mockProjectRepository.Verify(mock => mock.UpdateProjectAndMemberships(arg.Capture(), secondArg.Capture()), Times.Never());  
    }


    [Fact]       
    public void SaveChanges_WithValidFieldsAndNoLeader_ProjectNotUpdated() {
        CreateComponent(false);
        var name = "Test Project Name";
        var description = "Test Project Description";
        var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
        var endDate = startDate.AddDays(42);

        _component.Find("#name-input").Change(name);
        _component.Find("#description-input").Change(description);
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));       

        // Removing the only leader will not allow changes to be saved
        _component.Find("#role-changer-select-1").Change("Reviewer");     
        _component.Find("#edit-project-form").Submit(); 
            
        var arg = new ArgumentCaptor<Project>();
        var secondArg = new ArgumentCaptor<List<ProjectUserMembership>>();
        _mockProjectRepository.Verify(mock => mock.UpdateProjectAndMemberships(arg.Capture(), secondArg.Capture()), Times.Never());             
    }

    [Fact]       
    public void SaveChanges_WithValidFieldsAndSelect_ProjectUpdated() {
        CreateComponent(false);
        var name = "Test Project Name";
        var description = "Test Project Description";
        var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
        var endDate = startDate.AddDays(42);

        _component.Find("#name-input").Change(name);
        _component.Find("#description-input").Change(description);
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));       

        _component.Find($"#role-changer-select-{_anotherUser.Id}").Change("Reviewer");     
            
        SetupSave();
            
        _component.Find("#edit-project-form").Submit(); 
            
        var memberAssociationsArg = new ArgumentCaptor<List<ProjectUserMembership>>();
        _mockProjectRepository.Verify(mock => mock.UpdateProjectAndMemberships(It.IsAny<Project>(), memberAssociationsArg.Capture()), Times.Once());            
        memberAssociationsArg.Value.First(assoc => assoc.UserId == _anotherUser.Id).Role.Should().Be(ProjectRole.Reviewer);
        _component.FindAll("#project-concurrency-error").Should().BeEmpty();
    }

    [Theory]
    [InlineData("2020-11-14", "2022-12-12")]       
    [InlineData("2023-11-14", "2022-12-12")]  
    [InlineData("2084-11-14", "2085-12-12")]
    [InlineData("2021-11-14", "2022-12-12")]  
    [InlineData("2021-11-14", "2021-11-14")]        
    public void SaveChanges_WithInvalidDates_ProjectNotUpdated(string startDateInput, string endDateInput) {
        CreateComponent(false);
        var startDate = DateOnly.Parse(startDateInput);
        var endDate = DateOnly.Parse(endDateInput);
     
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));

        _component.Find("#edit-project-form").Submit();

        var arg = new ArgumentCaptor<Project>();
        var secondArg = new ArgumentCaptor<List<ProjectUserMembership>>();
        _mockProjectRepository.Verify(mock => mock.UpdateProjectAndMemberships(arg.Capture(), secondArg.Capture()), Times.Never());  
    }

    [Fact]
    public void CheckCredentials_CredentialsValid_CheckButtonSetToValid()
    {
        CreateComponent(false);
        var gitlabUrl = new Uri("http://example.com/test");
        var gitlabAccessToken = "AABB";
        var gitlabProjectId = 42;
        _mockGitlabService
            .Setup(mock => mock.TestCredentials(It.IsAny<GitlabCredentials>()))
            .Returns(Task.CompletedTask);

        _component.Find("#gitlab-enabled-checkbox").Change(true);

        _component.Find("#project-url-input").Change(gitlabUrl);
        _component.Find("#project-access-token-input").Change(gitlabAccessToken);
        _component.Find("#project-id-input").Change(gitlabProjectId);
            
        _component.Find("#check-gitlab-credentials").TextContent.Trim().Should().Be("Check");
        _component.Find("#check-gitlab-credentials").Click();
        _component.Find("#check-gitlab-credentials").TextContent.Trim().Should().Be("Valid");
    }
        
    [Fact]
    public void CheckCredentials_CredentialsNotValid_CheckButtonUnchanged()
    {
        CreateComponent(false);
        var gitlabUrl = new Uri("http://example.com/test");
        var gitlabAccessToken = "AABB";
        var gitlabProjectId = 42;
        _mockGitlabService
            .Setup(mock => mock.TestCredentials(It.IsAny<GitlabCredentials>()))
            .ThrowsAsync(new GitlabRequestFailedException(RequestFailure.ConnectionFailed));

        _component.Find("#gitlab-enabled-checkbox").Change(true);

        _component.Find("#project-url-input").Change(gitlabUrl);
        _component.Find("#project-access-token-input").Change(gitlabAccessToken);
        _component.Find("#project-id-input").Change(gitlabProjectId);
            
        _component.Find("#check-gitlab-credentials").TextContent.Trim().Should().Be("Check");
        _component.Find("#check-gitlab-credentials").Click();
        _component.Find("#check-gitlab-credentials").TextContent.Trim().Should().Be("Check");
    }

    [Fact]       
    public void SaveChanges_AnotherUserEdited_ErrorMessageDisplayed() {
        CreateComponent(false);
        var name = "Test Project Name";
        var description = "Test Project Description";
        var startDate = DateOnly.FromDateTime(DateTime.Now).AddDays(10);
        var endDate = startDate.AddDays(42);

        _component.Find("#name-input").Change(name);
        _component.Find("#description-input").Change(description);
        _component.Find("#start-date-input").Change(startDate.ToString("yyyy-MM-dd"));
        _component.Find("#end-date-input").Change(endDate.ToString("yyyy-MM-dd"));       

        _component.Find($"#role-changer-select-{_anotherUser.Id}").Change("Reviewer");     
            
        SetupSave();

        // Mock a concurrency exception
        _mockProjectRepository
            .Setup(mock =>
                mock.UpdateProjectAndMemberships(It.IsAny<Project>(), It.IsAny<List<ProjectUserMembership>>()))
            .Throws(new DbUpdateConcurrencyException("Concurrency Error"));
            
        _component.Find("#edit-project-form").Submit(); 
            
        _component.FindAll("#project-concurrency-error").Should().ContainSingle();
    }
        
    [Fact]       
    public void SaveChanges_ValidFields_CreatorIdAndCreatedFieldsUnchanged()
    {
        var projectId = _project.Id;
        var creatorId = 30;
        var created = DateTime.Now;
        _project.CreatorId = creatorId;
        _project.Created = created;
            
        CreateComponent(false);

        SetupSave();
            
        _component.Find("#edit-project-form").Submit();

        var captor = new ArgumentCaptor<Project>();
        _mockProjectRepository
            .Verify(mock => mock.UpdateProjectAndMemberships(captor.Capture(), It.IsAny<List<ProjectUserMembership>>()), Times.Once);
        var updatedProject = captor.Value;

        updatedProject.Id.Should().Be(projectId);
        updatedProject.CreatorId.Should().Be(creatorId);
        updatedProject.Creator.Should().BeNull();
        updatedProject.Created.Should().Be(created);
    }
}