using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.DataAccess;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Services;
using ScrumBoard.Tests.Integration.Infrastructure;
using ScrumBoard.Tests.Integration.LiveUpdating;
using ScrumBoard.Tests.Util;
using ScrumBoard.Tests.Util.LiveUpdating;
using Xunit;
using Xunit.Abstractions;

namespace ScrumBoard.Tests.Integration.Services;

[Collection(LiveUpdateIsolationCollection.CollectionName)]
public class FormInstanceServiceTest : MultipleHubConnectionBaseIntegrationTextFixture
{
    private readonly IFormInstanceService _formInstanceService;
    private readonly List<User> _users;
    private readonly List<Project> _projects;

    private readonly User _johnSmith = new() {Id = 101, FirstName = "John", LastName = "Smith"};
    private readonly User _secondUser = new() {Id = 102, FirstName = "Second", LastName = "User"};
    private readonly User _thirdUser = new() {Id = 103, FirstName = "Third", LastName = "User"};
    private readonly User _fourthUser = new() {Id = 104, FirstName = "Fourth", LastName = "User"};
    private readonly User _fifthUser = new() {Id = 105, FirstName = "Fifth", LastName = "User"};
    private readonly User _sixthUser = new() {Id = 106, FirstName = "Sixth", LastName = "User"};
    private readonly User _seventhUser = new() {Id = 107, FirstName = "Seventh", LastName = "User"};
    private readonly User _eighthUser = new() {Id = 108, FirstName = "Eighth", LastName = "User"};
    private readonly User _ninthUser = new() {Id = 109, FirstName = "Ninth", LastName = "User"};

    private readonly Project _firstProject = FakeDataGenerator.CreateFakeProject();
    private readonly Project _secondProject = FakeDataGenerator.CreateFakeProject();
    private readonly List<ProjectUserMembership> _projectUserMemberships;

    private readonly FormTemplate _formTemplate;
    private readonly TextQuestion _textQuestion;
    private readonly MultiChoiceQuestion _multiChoiceQuestion;
    private readonly MultiChoiceOption _firstOption, _secondOption;

    public FormInstanceServiceTest(TestWebApplicationFactory factory, ITestOutputHelper outputHelper) : base(factory, outputHelper)
    {
        _projects = [
            _firstProject,
            _secondProject
        ];
        
        _users = [
            _johnSmith,
            _secondUser,
            _thirdUser,
            _fourthUser,
            _fifthUser,
            _sixthUser,
            _seventhUser,
            _eighthUser,
            _ninthUser
        ];
        
        _projectUserMemberships = new List<ProjectUserMembership> 
        {
            new() {ProjectId = _firstProject.Id, UserId = _johnSmith.Id, Role = ProjectRole.Developer},
            new() {ProjectId = _firstProject.Id, UserId = _secondUser.Id, Role = ProjectRole.Developer},
            new() {ProjectId = _firstProject.Id, UserId = _thirdUser.Id, Role = ProjectRole.Developer},
            new() {ProjectId = _firstProject.Id, UserId = _fourthUser.Id, Role = ProjectRole.Developer},

            new() {ProjectId = _firstProject.Id, UserId = _seventhUser.Id, Role = ProjectRole.Leader},
            new() {ProjectId = _firstProject.Id, UserId = _eighthUser.Id, Role = ProjectRole.Leader},

            new() {ProjectId = _secondProject.Id, UserId = _fifthUser.Id, Role = ProjectRole.Developer},
            new() {ProjectId = _secondProject.Id, UserId = _sixthUser.Id, Role = ProjectRole.Developer},
            new() {ProjectId = _secondProject.Id, UserId = _ninthUser.Id, Role = ProjectRole.Developer}
        };
        
        _formTemplate = new FormTemplate
        {
            Id = 2000, Name = "MainForm", CreatorId = _johnSmith.Id
        };
        
        _textQuestion = new TextQuestion
        {
            Id = 1,
            FormTemplateId = _formTemplate.Id,
            FormPosition = 0,
            Prompt = "Hello, please answer this question:",
            MaxResponseLength = 100
        };

        _firstOption = new MultiChoiceOption { Id = 1, Content = "Option 1", BlockId = 2 };
        _secondOption = new MultiChoiceOption { Id = 2, Content = "Option 2", BlockId = 3 };
        _multiChoiceQuestion = new MultiChoiceQuestion
        {
            Id = 2,
            FormTemplateId = _formTemplate.Id,
            FormPosition = 0,
            Prompt = "Hello, please answer this multi-choice question:",
            AllowMultiple = false,
            Options = [ _firstOption, _secondOption ]
        };
        
        _formInstanceService = ServiceProvider.GetRequiredService<IFormInstanceService>();
    }

    protected override async Task SeedSampleDataAsync(DatabaseContext dbContext)
    {
        await base.SeedSampleDataAsync(dbContext);
        
        dbContext.Users.AddRange(_users);
        dbContext.Projects.AddRange(_projects);
        dbContext.ProjectUserMemberships.AddRange(_projectUserMemberships);

        dbContext.FormTemplates.Add(_formTemplate);
        dbContext.TextQuestions.Add(_textQuestion);
        dbContext.MultichoiceQuestions.Add(_multiChoiceQuestion);
        
        await dbContext.SaveChangesAsync();
    }

    private async Task<UserFormInstance> CreateUserFormInstance(
        User user, 
        Project project, 
        AssignmentType assignmentType=AssignmentType.Individual, 
        User pairUser=null,
        FormStatus status = FormStatus.Todo
    ) {
        var instance = new UserFormInstance
        {
            Assignment = new Assignment
            {
                FormTemplateId = _formTemplate.Id, 
                AssignmentType = assignmentType,
                Name = $"Test assignment just for {user.GetFullName()}"
            },
            AssigneeId = user.Id,
            ProjectId = project.Id,
            PairId = pairUser?.Id,
            Status = status
        };
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.FormInstances.Add(instance);
        await context.SaveChangesAsync();
        return instance;
    }
    
    private async Task<TeamFormInstance> CreateTeamFormInstance(Project assignee, Project target)
    {
        var instance = new TeamFormInstance()
        {
            Assignment = new Assignment
            {
                FormTemplateId = _formTemplate.Id, 
                AssignmentType = AssignmentType.Team,
                Name = $"Test assignment just for {assignee.Name} who is reviewing {target.Name}"
            },
            ProjectId = assignee.Id,
            LinkedProjectId = target.Id
        };
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        context.FormInstances.Add(instance);
        await context.SaveChangesAsync();
        return instance;
    }
    
    private FormTemplateAssignmentForm CreateTeamFormTemplateAssignmentForm(IEnumerable<LinkedProjects> formLinkedProjects)
    {
        var selectedRoles = Enum.GetValues<ProjectRole>()
            .ToDictionary(x => x, y => y is ProjectRole.Developer or ProjectRole.Leader);

        var form = new FormTemplateAssignmentForm
        {
            FormTemplate = _formTemplate,
            SelectedLinkedProjects = formLinkedProjects,
            Name = "New team form instance name",
            EndDate = DateTime.Now.AddDays(1),
            SelectedRoles = selectedRoles,
            AssignmentType = AssignmentType.Team
        };
        return form;
    }

    private FormTemplateAssignmentForm CreateUserFormTemplateAssignmentForm(
        bool pairwise, 
        List<ProjectRole> formRoles,
        IEnumerable<Project> formProjects
    ) {
        var selectedRoles = Enum.GetValues<ProjectRole>().ToDictionary(x => x, _ => false);

        foreach (var role in formRoles) selectedRoles[role] = true;

        var assignmentType = pairwise ? AssignmentType.Pairwise : AssignmentType.Individual;

        var form = new FormTemplateAssignmentForm
        {
            FormTemplate = _formTemplate,
            SelectedSingleProjects = formProjects,
            Name = "New form instance name",
            EndDate = DateTime.Now.AddDays(1),
            SelectedRoles = selectedRoles,
            AssignmentType = assignmentType
        };
        return form;
    }

    private static List<Assignment> CreateAssignments()
    {
        return new List<Assignment>
        {
            new()
            {
                Name = "Sprint 1 Self-Reflection",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(4),
                RunNumber = 0,
                Instances = new List<FormInstance>()
            },
            new()
            {
                Name = "Sprint 2 Self-Reflection",
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(6),
                RunNumber = 1,
                Instances = new List<FormInstance>()
            },
            new()
            {
                Name = "Sprint 3 Self-Reflection",
                StartDate = DateTime.Now.AddDays(2),
                EndDate = DateTime.Now.AddDays(7),
                RunNumber = 2,
                Instances = new List<FormInstance>()
            },
            new()
            {
                Name = "Sprint 4 Self-Reflection",
                StartDate = DateTime.Now.AddDays(2),
                EndDate = DateTime.Now.AddDays(7),
                RunNumber = 3,
                Instances = new List<FormInstance>()
            },
            new()
            {
                Name = "Sprint 5 Self-Reflection",
                StartDate = DateTime.Now.AddDays(2),
                EndDate = DateTime.Now.AddDays(7),
                RunNumber = 4,
                Instances = new List<FormInstance>()
            },
            new()
            {
                Name = "Sprint 6 Self-Reflection",
                StartDate = DateTime.Now.AddDays(2),
                EndDate = DateTime.Now.AddDays(7),
                RunNumber = 5,
                Instances = new List<FormInstance>()
            },
        };
    }

    [Fact]
    public async Task UserAssignsForm_IndividualForm_FormInstancesAreCreated()
    {
        var project = _firstProject;
        List<User> developersInProject;

        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.FormTemplates.Should().HaveCount(1);
            context.TextQuestions.Should().HaveCount(1);
            context.MultichoiceQuestions.Should().HaveCount(1);

            context.UserFormInstances.Should().HaveCount(0);
            context.TextAnswers.Should().HaveCount(0);
            context.MultichoiceAnswers.Should().HaveCount(0);

            developersInProject = context.ProjectUserMemberships
                .Where(m => m.Project == project && m.Role == ProjectRole.Developer).Select(m => m.User).ToList();
        }

        var expectedFormInstancesCount = developersInProject.Count;

        var form = CreateUserFormTemplateAssignmentForm(false, new List<ProjectRole> {ProjectRole.Developer},
            new List<Project> {_firstProject});
        await _formInstanceService.CreateAndAssignFormInstancesAsync(form);

        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.UserFormInstances.Should().HaveCount(expectedFormInstancesCount);

            foreach (var developer in developersInProject)
            {
                context.UserFormInstances.Count(i => developer.Id == i.AssigneeId).Should()
                    .Be(1);
            }
        }
    }
    
    [Fact]
    public async Task UserAssignsForm_TeamForm_FormInstancesAreCreated()
    {
        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.FormTemplates.Should().HaveCount(1);
            context.TextQuestions.Should().HaveCount(1);
            context.MultichoiceQuestions.Should().HaveCount(1);

            context.TeamFormInstances.Should().HaveCount(0);
            context.TextAnswers.Should().HaveCount(0);
            context.MultichoiceAnswers.Should().HaveCount(0);
        }

        var firstLinkedProject = new LinkedProjects
        {
            FirstProject = _firstProject,
            SecondProject = _secondProject
        };

        var secondLinkedProject = new LinkedProjects
        {
            FirstProject = _secondProject,
            SecondProject = _firstProject
        };

        var form = CreateTeamFormTemplateAssignmentForm([firstLinkedProject, secondLinkedProject]);
        await _formInstanceService.CreateAndAssignFormInstancesAsync(form);

        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.TeamFormInstances.Should().HaveCount(2);

            context.TeamFormInstances.First().ProjectId.Should().Be(_firstProject.Id);
            context.TeamFormInstances.First().LinkedProjectId.Should().Be(_secondProject.Id);
            
            context.TeamFormInstances.First(i => i.ProjectId == _firstProject.Id)
                .LinkedProjectId.Should().Be(_secondProject.Id);
            context.TeamFormInstances.First(i => i.ProjectId == _secondProject.Id)
                .LinkedProjectId.Should().Be(_firstProject.Id);
        }
    }

    [Fact]
    public async Task UserAssignsForm_PairwiseForm_FormInstancesAreCreated()
    {
        var project = _firstProject;
        List<User> developersInProject;

        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.FormTemplates.Should().HaveCount(1);
            context.TextQuestions.Should().HaveCount(1);
            context.MultichoiceQuestions.Should().HaveCount(1);

            context.UserFormInstances.Should().HaveCount(0);
            context.TextAnswers.Should().HaveCount(0);
            context.MultichoiceAnswers.Should().HaveCount(0);

            developersInProject = context.ProjectUserMemberships
                .Where(m => m.Project == project && m.Role == ProjectRole.Developer).Select(m => m.User).ToList();
        }

        var expectedFormInstancesCount = developersInProject.Count * (developersInProject.Count - 1);

        var form = CreateUserFormTemplateAssignmentForm(true, new List<ProjectRole> {ProjectRole.Developer},
            new List<Project> {project});
        await _formInstanceService.CreateAndAssignFormInstancesAsync(form);

        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.UserFormInstances.Should().HaveCount(expectedFormInstancesCount);

            foreach (var developer in developersInProject)
            {
                context.UserFormInstances.Count(i => developer.Id == i.AssigneeId).Should()
                    .Be(developersInProject.Count - 1);
            }
            
        }
    }

    [Fact]
    public async Task UserAssignsForm_MultipleProjects_FormInstancesAreCreated()
    {
        var projects = new List<Project> {_firstProject, _secondProject};
        List<User> developersInProject1;
        List<User> developersInProject2;

        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.FormTemplates.Should().HaveCount(1);
            context.TextQuestions.Should().HaveCount(1);
            context.MultichoiceQuestions.Should().HaveCount(1);

            context.UserFormInstances.Should().HaveCount(0);
            context.TextAnswers.Should().HaveCount(0);
            context.MultichoiceAnswers.Should().HaveCount(0);
            developersInProject1 = context.ProjectUserMemberships
                .Where(m => m.Project == projects[0] && m.Role == ProjectRole.Developer).Select(m => m.User).ToList();
            developersInProject2 = context.ProjectUserMemberships
                .Where(m => m.Project == projects[1] && m.Role == ProjectRole.Developer).Select(m => m.User).ToList();
        }

        var expectedFormInstancesCount = developersInProject1.Count + developersInProject2.Count;

        var form = CreateUserFormTemplateAssignmentForm(false, new List<ProjectRole> {ProjectRole.Developer},
            projects);
        await _formInstanceService.CreateAndAssignFormInstancesAsync(form);

        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.UserFormInstances.Should().HaveCount(expectedFormInstancesCount);

            foreach (var developer in developersInProject1)
            {
                context.UserFormInstances.Count(i => developer.Id == i.AssigneeId).Should().Be(1);
            }

            foreach (var developer in developersInProject2)
            {
                context.UserFormInstances.Count(i => developer.Id == i.AssigneeId).Should().Be(1);
            }
            
        }
    }

    [Fact]
    public async Task UserAssignsForm_MultipleRoles_FormInstancesAreCreated()
    {
        var project = _firstProject;

        List<User> developersAndLeaders;
        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.FormTemplates.Should().HaveCount(1);
            context.TextQuestions.Should().HaveCount(1);
            context.MultichoiceQuestions.Should().HaveCount(1);

            context.UserFormInstances.Should().HaveCount(0);
            context.TextAnswers.Should().HaveCount(0);
            context.MultichoiceAnswers.Should().HaveCount(0);

            developersAndLeaders = context.ProjectUserMemberships
                .Where(m => m.Project == project && (m.Role == ProjectRole.Developer || m.Role == ProjectRole.Leader))
                .Select(m => m.User).ToList();
        }

        var form = CreateUserFormTemplateAssignmentForm(false,
            new List<ProjectRole> {ProjectRole.Developer, ProjectRole.Leader},
            new List<Project> {project});
        await _formInstanceService.CreateAndAssignFormInstancesAsync(form);

        using (new AssertionScope())
        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.UserFormInstances.Should().HaveCount(developersAndLeaders.Count);

            foreach (var user in developersAndLeaders)
            {
                context.UserFormInstances.Count(i => user.Id == i.AssigneeId).Should().Be(1);
            }
            
        }
    }

    [Fact]
    public async Task UserAssignsForm_MultipleProjectsPairwise_FormInstancesAreCreated()
    {
        var projects = new List<Project> {_firstProject, _secondProject};
        List<User> developersInProject1;
        List<User> developersInProject2;

        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.FormTemplates.Should().HaveCount(1);
            context.TextQuestions.Should().HaveCount(1);
            context.MultichoiceQuestions.Should().HaveCount(1);

            context.UserFormInstances.Should().HaveCount(0);
            context.TextAnswers.Should().HaveCount(0);
            context.MultichoiceAnswers.Should().HaveCount(0);

            developersInProject1 = context.ProjectUserMemberships
                .Where(m => m.Project == projects[0] && m.Role == ProjectRole.Developer).Select(m => m.User).ToList();
            developersInProject2 = context.ProjectUserMemberships
                .Where(m => m.Project == projects[1] && m.Role == ProjectRole.Developer).Select(m => m.User).ToList();
        }

        var numDevelopersInProject1 = developersInProject1.Count;
        var numDevelopersInProject2 = developersInProject2.Count;
        var expectedFormInstancesCount = numDevelopersInProject1 * (numDevelopersInProject1 - 1) +
                                         numDevelopersInProject2 * (numDevelopersInProject2 - 1);

        var form = CreateUserFormTemplateAssignmentForm(true, new List<ProjectRole> {ProjectRole.Developer},
            projects);
        await _formInstanceService.CreateAndAssignFormInstancesAsync(form);

        using (new AssertionScope())
        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.UserFormInstances.Should().HaveCount(expectedFormInstancesCount);

            foreach (var developer in developersInProject1)
            {
                context.UserFormInstances.Count(i => developer.Id == i.AssigneeId).Should()
                    .Be(numDevelopersInProject1 - 1);
            }

            foreach (var developer in developersInProject2)
            {
                context.UserFormInstances.Count(i => developer.Id == i.AssigneeId).Should()
                    .Be(numDevelopersInProject2 - 1);
            }
        }
    }

    [Fact]
    public async Task UserAssignsForm_UserAssignsTheFormAgain_RunNumberIncreases()
    {
        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.FormTemplates.Should().HaveCount(1);
            context.TextQuestions.Should().HaveCount(1);
            context.MultichoiceQuestions.Should().HaveCount(1);

            context.UserFormInstances.Should().HaveCount(0);
            (await context.FormTemplates.FindAsync(_formTemplate.Id))!.RunNumber.Should().Be(0);
        }

        var form = CreateUserFormTemplateAssignmentForm(false, new List<ProjectRole> {ProjectRole.Developer},
            new List<Project> {_firstProject});
        await _formInstanceService.CreateAndAssignFormInstancesAsync(form);

        long initialRunNumber;

        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            initialRunNumber = context.UserFormInstances.AsQueryable().Include(i => i.Assignment)
                .First(x => x.ProjectId == _firstProject.Id)
                .Assignment.RunNumber;
        }

        var secondForm = CreateUserFormTemplateAssignmentForm(false, new List<ProjectRole> {ProjectRole.Developer},
            new List<Project> {_secondProject});
        await _formInstanceService.CreateAndAssignFormInstancesAsync(secondForm);

        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.UserFormInstances.AsQueryable().Include(i => i.Assignment)
                .First(x => x.ProjectId == _secondProject.Id).Assignment.RunNumber
                .Should().Be(initialRunNumber + 1);
        }
    }


    [Fact]
    public async Task GetPaginatedAssignments_FirstPage_CorrectAssignmentsReturned()
    {
        _formTemplate.Assignments = CreateAssignments();
        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.FormTemplates.Update(_formTemplate);
            await context.SaveChangesAsync();
        }

        var paginatedAssignments = await _formInstanceService.GetPaginatedAssignments(_formTemplate.Id, 1);
        paginatedAssignments.Count.Should().Be(5);
        var paginatedAssignmentRunNumbers = paginatedAssignments.Select(a => a.RunNumber);

        var expectedAssignments = _formTemplate.Assignments.OrderByDescending(x => x.RunNumber).Take(5);
        var expectedRunNumbers = expectedAssignments.Select(a => a.RunNumber);

        expectedRunNumbers.Should().BeEquivalentTo(paginatedAssignmentRunNumbers);
    }

    [Fact]
    public async Task GetPaginatedAssignments_SecondPage_CorrectAssignmentsReturned()
    {
        _formTemplate.Assignments = CreateAssignments();
        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.FormTemplates.Update(_formTemplate);
            await context.SaveChangesAsync();
        }

        var paginatedAssignments = await _formInstanceService.GetPaginatedAssignments(_formTemplate.Id, 2);
        paginatedAssignments.Count.Should().Be(1);
        var paginatedAssignmentRunNumbers = paginatedAssignments.Select(a => a.RunNumber);

        var expectedAssignments = new List<Assignment>
        {
            _formTemplate.Assignments.OrderByDescending(x => x.RunNumber).Last()
        };
        var expectedRunNumbers = expectedAssignments.Select(a => a.RunNumber);

        expectedRunNumbers.Should().BeEquivalentTo(paginatedAssignmentRunNumbers);
    }

    [Fact]
    private async Task GetUserFormsForUserInProject_OnlyTeamFormsAndFormsForOtherUsers_NoneReturned()
    {
        await CreateTeamFormInstance( _firstProject, _secondProject);
        await CreateUserFormInstance(_secondUser, _firstProject);
        var forms = await _formInstanceService.GetUserFormsForUserInProject(_johnSmith.Id, _firstProject.Id);
        forms.Should().BeEmpty();
    }

    [Fact]
    private async Task GetUserFormsForUserInProject_FormsExist_CorrectFormsReturned()
    {
        var expected = await CreateUserFormInstance(_johnSmith, _firstProject);
        var forms = await _formInstanceService.GetUserFormsForUserInProject(_johnSmith.Id, _firstProject.Id);
        forms.Should().ContainSingle().Which.Id.Should().Be(expected.Id);
    }
    
    [Fact]
    private async Task GetTeamFormsForProject_OnlyUserFormsAndFormsForOtherProjects_NoneReturned()
    {
        await CreateTeamFormInstance(_secondProject, _firstProject);
        await CreateUserFormInstance(_johnSmith, _firstProject);
        var forms = await _formInstanceService.GetTeamFormsForProject(_firstProject.Id);
        forms.Should().BeEmpty();
    }

    [Fact]
    private async Task GetTeamFormsForProject_FormsExist_CorrectFormsReturned()
    {
        var expected = await CreateTeamFormInstance(_firstProject, _secondProject);
        var forms = await _formInstanceService.GetTeamFormsForProject(_firstProject.Id);
        forms.Should().ContainSingle().Which.Id.Should().Be(expected.Id);
    }

    [Fact]
    private async Task GetUserFormInstanceById_NoSuchForm_NullReturned()
    {
        var form = await _formInstanceService.GetUserFormInstanceById(FakeDataGenerator.NextId);
        form.Should().BeNull();
    }
    
    [Fact]
    private async Task GetUserFormInstanceById_FormInstanceExists_FormReturnedCorrectly()
    {
        var expected = await CreateUserFormInstance(_johnSmith, _firstProject);
        var form = await _formInstanceService.GetUserFormInstanceById(expected.Id);
        form.Id.Should().Be(expected.Id);
    }
    
    [Fact]
    private async Task GetTeamFormInstanceById_NoSuchForm_NullReturned()
    {
        var form = await _formInstanceService.GetTeamFormInstanceById(FakeDataGenerator.NextId);
        form.Should().BeNull();
    }
    
    [Fact]
    private async Task GetTeamFormInstanceById_FormInstanceExists_FormReturnedCorrectly()
    {
        var expected = await CreateTeamFormInstance(_firstProject, _secondProject);
        var form = await _formInstanceService.GetTeamFormInstanceById(expected.Id);
        form.Id.Should().Be(expected.Id);
    }
    
    [Fact]
    private async Task SaveAnswerToTextFormBlock_NoExistingAnswer_NewAnswerCreatedSuccessfully()
    {
        var instance = await CreateUserFormInstance(_johnSmith, _firstProject);
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        
        context.Answers.Should().BeEmpty();
        
        await _formInstanceService.SaveAnswerToTextFormBlock(
            instance.Id, 
            _textQuestion.Id, 
            "Answer", 
            _johnSmith.Id, 
            false
        );

        context.TextAnswers
            .Should().ContainSingle()
            .Which.Answer
            .Should().Be("Answer");
    }
    
    [Fact]
    private async Task SaveAnswerToTextFormBlock_ExistingAnswer_AnswerUpdatedSuccessfully()
    {
        var instance = await CreateUserFormInstance(_johnSmith, _firstProject);
        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.Answers.Add(new TextAnswer { Answer = "Old", QuestionId = _textQuestion.Id, FormInstanceId = instance.Id });
            await context.SaveChangesAsync();
        }
        
        await _formInstanceService.SaveAnswerToTextFormBlock(
            instance.Id,
            _textQuestion.Id,
            "Answer",
            _johnSmith.Id,
            false
        );

        await using var newContext = await GetDbContextFactory().CreateDbContextAsync();
        newContext.TextAnswers
            .Should().ContainSingle()
            .Which.Answer
            .Should().Be("Answer");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("StartHubConnection", "true")]
    private async Task SaveAnswerToTextFormBlock_BothBroadcastTypes_BroadcastsChangesToCorrectConnections(bool shouldBroadcast)
    {
        var instance = await CreateUserFormInstance(User1InProjectB, ProjectB);
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        await CreateAndStartHubConnections();

        LiveUpdateEventInvocations.Clear();
        await _formInstanceService.SaveAnswerToTextFormBlock(
            instance.Id,
            _textQuestion.Id,
            "Answer",
            User1InProjectB.Id,
            shouldBroadcast
        );

        // Make sure that all expected events have been received: 2 when only broadcasting to current user, 4 if broadcasting to whole project
        await WaitForLiveUpdateInvocationsAssertionToPass(x => x.Count == (shouldBroadcast ? 4 : 2));

        InvocationsReceivedByUser1InProjectB
            .Where(x => x.EntityId == instance.Id)
            .Where(x => x.EventType is LiveUpdateEventType.EntityUpdated or LiveUpdateEventType.EntityHasChanged)
            .Should().HaveCount(2);

        InvocationsReceivedByUser2InProjectB
            .Where(x => x.EntityId == instance.Id)
            .Where(x => x.EventType is LiveUpdateEventType.EntityUpdated or LiveUpdateEventType.EntityHasChanged)
            .Should().HaveCount(shouldBroadcast ? 2 : 0);
    }
    
    [Fact]
    private async Task SaveAnswerToMultiChoiceFormBlock_FormInstanceAlreadySubmitted_ExceptionThrown()
    {
        var instance = await CreateUserFormInstance(_johnSmith, _firstProject, status: FormStatus.Submitted);

        var action = async () => await _formInstanceService.SaveAnswerToMultiChoiceFormBlock(
            instance.Id, 
            _multiChoiceQuestion.Id, 
            [ _firstOption.Id ], 
            _johnSmith.Id, 
            false
        );
        await action.Should().ThrowExactlyAsync<InvalidOperationException>();
    }
    
    [Fact]
    private async Task SaveAnswerToMultiChoiceFormBlock_NoSuchQuestionWithId_ExceptionThrown()
    {
        var instance = await CreateUserFormInstance(_johnSmith, _firstProject);

        var action = async () => await _formInstanceService.SaveAnswerToMultiChoiceFormBlock(
            instance.Id, 
            FakeDataGenerator.NextId, 
            [ _firstOption.Id ], 
            _johnSmith.Id, 
            false
        );
        await action.Should().ThrowExactlyAsync<ArgumentException>();
    }
    
    [Fact]
    private async Task SaveAnswerToMultiChoiceFormBlock_MultipleOptionsSelectedWhenOnlySingleAllowed_ExceptionThrown()
    {
        var instance = await CreateUserFormInstance(_johnSmith, _firstProject);

        var action = async () => await _formInstanceService.SaveAnswerToMultiChoiceFormBlock(
            instance.Id, 
            _multiChoiceQuestion.Id, 
            [ _firstOption.Id, _secondOption.Id ], 
            _johnSmith.Id, 
            false
        );
        await action.Should().ThrowExactlyAsync<ActionNotSupportedException>();
    }
    
    [Fact]
    private async Task SaveAnswerToMultiChoiceFormBlock_NoExistingAnswer_NewAnswerCreatedSuccessfully()
    {
        var instance = await CreateUserFormInstance(_johnSmith, _firstProject);
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        
        context.Answers.Should().BeEmpty();
        
        await _formInstanceService.SaveAnswerToMultiChoiceFormBlock(
            instance.Id, 
            _multiChoiceQuestion.Id, 
            [ _firstOption.Id ], 
            _johnSmith.Id, 
            false
        );

        context.MultichoiceAnswers
            .Should().ContainSingle().Which
            .SelectedOptions.Should().ContainSingle().Which
            .MultichoiceOptionId.Should().Be(_firstOption.Id);
    }
    
    [Fact]
    private async Task SaveAnswerToMultiChoiceFormBlock_ExistingAnswer_AnswerUpdatedSuccessfully()
    {
        var instance = await CreateUserFormInstance(_johnSmith, _firstProject);
        await using (var context = await GetDbContextFactory().CreateDbContextAsync())
        {
            context.MultichoiceAnswers.Add(new MultiChoiceAnswer
            {
                QuestionId = _multiChoiceQuestion.Id, 
                FormInstanceId = instance.Id,
                SelectedOptions = [ 
                    new MultichoiceAnswerMultichoiceOption
                    {
                        MultichoiceOptionId = _firstOption.Id
                    } 
                ]
            });
            await context.SaveChangesAsync();
        }
        
        await _formInstanceService.SaveAnswerToMultiChoiceFormBlock(
            instance.Id, 
            _multiChoiceQuestion.Id, 
            [ _secondOption.Id ], 
            _johnSmith.Id, 
            false
        );

        await using var newContext = await GetDbContextFactory().CreateDbContextAsync();
        newContext.MultichoiceAnswers
            .Should().ContainSingle().Which
            .SelectedOptions.Should().ContainSingle().Which
            .MultichoiceOptionId.Should().Be(_secondOption.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("StartHubConnection", "true")]
    private async Task SaveAnswerToMultiChoiceFormBlock_BothBroadcastTypes_BroadcastsChangesToCorrectConnections(bool shouldBroadcast)
    {
        var instance = await CreateUserFormInstance(User1InProjectB, ProjectB);
        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        await CreateAndStartHubConnections();

        LiveUpdateEventInvocations.Clear();
        await _formInstanceService.SaveAnswerToMultiChoiceFormBlock(
            instance.Id, 
            _multiChoiceQuestion.Id, 
            [ _firstOption.Id ], 
            User1InProjectB.Id, 
            shouldBroadcast
        );

        // Make sure that all expected events have been received: 2 when only broadcasting to current user, 4 if broadcasting to whole project
        await WaitForLiveUpdateInvocationsAssertionToPass(x => x.Count == (shouldBroadcast ? 4 : 2));

        InvocationsReceivedByUser1InProjectB
            .Where(x => x.EntityId == instance.Id)
            .Where(x => x.EventType is LiveUpdateEventType.EntityUpdated or LiveUpdateEventType.EntityHasChanged)
            .Should().HaveCount(2);

        InvocationsReceivedByUser2InProjectB
            .Where(x => x.EntityId == instance.Id)
            .Where(x => x.EventType is LiveUpdateEventType.EntityUpdated or LiveUpdateEventType.EntityHasChanged)
            .Should().HaveCount(shouldBroadcast ? 2 : 0);
    }

    [Fact]
    private async Task SubmitForm_FormExists_FormSubmittedAndSubmittedDateSet()
    {
        var now = DateTime.Now;
        ClockMock.Setup(x => x.Now).Returns(now);
        var formInstance = await CreateUserFormInstance(_johnSmith, _firstProject);
        await _formInstanceService.SubmitForm(formInstance.Id);

        await using var context = await GetDbContextFactory().CreateDbContextAsync();
        var instanceInDb = await context.FormInstances.FirstAsync(x => x.Id == formInstance.Id);

        instanceInDb.Status.Should().Be(FormStatus.Submitted);
        instanceInDb.SubmittedDate.Should().Be(now);
    }
    
    [Fact]
    private async Task SubmitForm_FormDoesNotExist_ExceptionThrown()
    {
        var action = async () => await _formInstanceService.SubmitForm(FakeDataGenerator.NextId);
        await action.Should().ThrowExactlyAsync<ArgumentException>();
    }

    [Fact]
    private async Task GetAnswerByFormInstanceAndQuestionId_AnswerExists_AnswerReturned()
    {
        var formInstance = await CreateUserFormInstance(_johnSmith, _firstProject);
        await _formInstanceService.SaveAnswerToTextFormBlock(
            formInstance.Id, 
            _textQuestion.Id, 
            "Hello", 
            _johnSmith.Id, 
            false
        );
        var answer = await _formInstanceService.GetAnswerByFormInstanceAndQuestionIdAsync(formInstance.Id, _textQuestion.Id);
        answer
            .Should().BeOfType<TextAnswer>()
            .Which
            .Answer.Should().Be("Hello");
    }
    
    [Fact]
    private async Task GetAnswerByFormInstanceAndQuestionId_AnswerDoesNotExist_NullReturned()
    {
        var answer = await _formInstanceService.GetAnswerByFormInstanceAndQuestionIdAsync(FakeDataGenerator.NextId, _textQuestion.Id);
        answer.Should().BeNull();
    }
}