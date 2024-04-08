using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Extensions;
using ScrumBoard.LiveUpdating;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Repositories;
using ScrumBoard.Utils;
using SharedLensResources.Blazor.Util;

namespace ScrumBoard.Services;

using AssignmentTransformType = Func<IQueryable<Assignment>, IQueryable<Assignment>>;

public interface IFormInstanceService
{
    /// <summary>
    /// Creates a set of form instances along with an Assignment for either single or pairwise FormTemplateAssignmentForms.
    /// </summary>
    /// <param name="form">A FormTemplateAssignmentForm used to generate all form instances</param>
    Task CreateAndAssignFormInstancesAsync(FormTemplateAssignmentForm form);
    
    /// <summary>
    /// Gets a list of ProjectUserMemberships for the users in the given project with the selected roles.
    /// </summary>
    /// <param name="projectId">A project ID to get recipients in</param>
    /// <param name="selectedRoles">A dictionary of ProjectRole->Bool where a true value means users with that role should be returned</param>
    /// <returns>An IList of ProjectUserMembership</returns>
    Task<IList<ProjectUserMembership>> GetRecipients(long projectId, IDictionary<ProjectRole, bool> selectedRoles);
    
    /// <summary>
    /// Get all user form instances for the given user in a specific project
    /// </summary>
    /// <param name="userId">A user ID to get forms for</param>
    /// <param name="projectId">The project to search in</param>
    /// <returns>An IEnumerable of user form instances</returns>
    Task<IEnumerable<UserFormInstance>> GetUserFormsForUserInProject(long userId, long projectId);
    
    /// <summary>
    /// Get all team form instances for the given user in a specific project
    /// </summary>
    /// <param name="projectId">The project to search in</param>
    /// <returns>An IEnumerable of team form instances</returns>
    Task<IEnumerable<TeamFormInstance>> GetTeamFormsForProject(long projectId);
    
    /// <summary>
    /// Return a single user form instance using its ID.
    /// </summary>
    /// <param name="formInstanceId">The ID of the form instance to return</param>
    /// <returns>The requested form instance</returns>
    Task<UserFormInstance> GetUserFormInstanceById(long formInstanceId);
    
    /// <summary>
    /// Return a single team form instance using its ID.
    /// </summary>
    /// <param name="formInstanceId">The ID of the form instance to return</param>
    /// <returns>The requested form instance</returns>
    Task<TeamFormInstance> GetTeamFormInstanceById(long formInstanceId);
    
    /// <summary>
    /// Saves a form instance and all answers from the given forms. Also sets the instance status to 'Submitted'.
    /// </summary>
    /// <param name="formInstanceId">The ID of the form instance to set as submitted</param>
    Task SubmitForm(long formInstanceId);

    /// <summary>
    /// Save the answer to a text question inside some form instance. Automatically broadcasts the changes to all live update
    /// connections of the user who made the changes, and optionally broadcasts the changes to all live update connections of
    /// other users in the same project if <see cref="shouldChangesBeBroadcastToWholeProject"/> is True.
    ///
    /// If the changes should be treated as confidential, for example in the case of a user filling out an individual form which
    /// only they may access (such as a self-reflection or similar), then set <see cref="shouldChangesBeBroadcastToWholeProject"/>
    /// to False.
    /// </summary>
    /// <param name="formInstanceId">The ID of the form instance to which the answer belongs</param>
    /// <param name="questionId">The ID of the <see cref="TextQuestion"/> for which an answer is being saved</param>
    /// <param name="textAnswer">The string content of the answer to be saved.</param>
    /// <param name="idOfEditingUser">The ID of the user who is making the change</param>
    /// <param name="shouldChangesBeBroadcastToWholeProject">
    /// Whether or not this change should be broadcast as a live update to all users in the project. Only set this field to true
    /// in cases where the answer being submitted is not confidential to the user submitting it.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown if the form instance has already been submitted</exception>
    Task SaveAnswerToTextFormBlock(long formInstanceId, long questionId, string textAnswer, long idOfEditingUser, bool shouldChangesBeBroadcastToWholeProject);

    /// <summary>
    /// Save the answer to a multi-choice question inside some form instance. Automatically broadcasts the changes to all live update
    /// connections of the user who made the changes, and optionally broadcasts the changes to all live update connections of
    /// other users in the same project if <see cref="shouldChangesBeBroadcastToWholeProject"/> is True.
    /// 
    /// If the changes should be treated as confidential, for example in the case of a user filling out an individual form which
    /// only they may access (such as a self-reflection or similar), then set <see cref="shouldChangesBeBroadcastToWholeProject"/>
    /// to False.
    /// </summary>
    /// <param name="formInstanceId">The ID of the form instance to which the answer belongs</param>
    /// <param name="questionId">The ID of the <see cref="TextQuestion"/> for which an answer is being saved</param>
    /// <param name="multiChoiceOptionIds">
    /// The IDs of the options to mark as selected for this question. If the question is not configured to allow multiple selections
    /// and multiple IDs are given here, then an <see cref="InvalidOperationException"/> is thrown.
    /// </param>
    /// <param name="idOfEditingUser">The ID of the user who is making the change</param>
    /// <param name="shouldChangesBeBroadcastToWholeProject">
    /// Whether or not this change should be broadcast as a live update to all users in the project. Only set this field to true
    /// in cases where the answer being submitted is not confidential to the user submitting it.
    /// </param>
    /// <exception cref="ActionNotSupportedException">
    /// Thrown when passing multiple choice option IDs to a question that only supports a single option to be chosen
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown if the form instance has already been submitted</exception>
    Task SaveAnswerToMultiChoiceFormBlock(long formInstanceId, long questionId, ICollection<long> multiChoiceOptionIds, long idOfEditingUser, bool shouldChangesBeBroadcastToWholeProject);
    
    /// <summary>
    /// Gets a paginated list of Assignments for the given form template.
    /// </summary>
    /// <param name="formTemplateId">The form template to get the assignments for</param>
    /// <param name="pageIndex">The page of assignments to get</param>
    /// <returns></returns>
    Task<PaginatedList<Assignment>> GetPaginatedAssignments(long formTemplateId, int pageIndex);

    /// <summary>
    /// Retrieves an answer belonging to a form instance and question by given IDs, if it exists.
    /// </summary>
    /// <param name="formInstanceId">ID of the form instance to which the answer belongs</param>
    /// <param name="questionId">ID of the question to which the answer belongs</param>
    /// <returns>Answer for question in form instance if it exists, null otherwise</returns>
    Task<Answer> GetAnswerByFormInstanceAndQuestionIdAsync(long formInstanceId, long questionId);
    
    Task<Assignment> GetAssignmentByIdAsync(long assignmentId);

    Task<ICollection<FormInstance>> GetFormInstancesForProjectAndAssignmentAsync(long projectId, long assignmentId);

    Task<ICollection<Assignment>> GetAllAssignmentsForProjectAsync(long projectId);
}

public class FormInstanceService : IFormInstanceService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IFormTemplateRepository _formTemplateRepository;
    private readonly IAssignmentRepository _assignmentRepository;

    private const int PageSize = 5;

    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IEntityLiveUpdateService _entityLiveUpdateService;
    private readonly IClock _clock;
    
    public FormInstanceService(
        IProjectRepository projectRepository,
        IFormTemplateRepository formTemplateRepository,
        IAssignmentRepository assignmentRepository,
        IDbContextFactory<DatabaseContext> dbContextFactory, 
        IEntityLiveUpdateService entityLiveUpdateService, 
        IClock clock
    ) {
        _formTemplateRepository = formTemplateRepository;
        _assignmentRepository = assignmentRepository;
        _projectRepository = projectRepository;
        _dbContextFactory = dbContextFactory;
        _entityLiveUpdateService = entityLiveUpdateService;
        _clock = clock;
    }
    
    /// <inheritdoc />
    public async Task CreateAndAssignFormInstancesAsync(FormTemplateAssignmentForm form)
    {
        form.StartDate = form.StartDate.GetValueOrDefault(_clock.Now).Floor(TimeSpan.FromMinutes(1));
        form.EndDate = form.EndDate!.Value.Floor(TimeSpan.FromMinutes(1));

        var template = await _formTemplateRepository.GetByIdAsync(form.FormTemplate.Id, FormTemplateIncludes.Blocks);
        
        var formInstances = await CreateFormInstancesAsync(form, template);
        
        var assignment = new Assignment
        {
            FormTemplate = template,
            Name = form.Name,
            StartDate = form.StartDate.GetValueOrDefault(_clock.Now),
            EndDate = form.EndDate!.Value,
            RunNumber = template.RunNumber,
            Instances = new List<FormInstance>(),
            AllowSavingBeforeStartDate = form.AllowSavingBeforeStartDate,
            AssignmentType = form.AssignmentType
        };
        foreach (var instance in formInstances)
        {
            assignment.Instances.Add(instance);
        }

        template.RunNumber++;
        await _assignmentRepository.UpdateAsync(assignment);
    }

    private async Task<List<FormInstance>> CreateFormInstancesAsync(FormTemplateAssignmentForm form, FormTemplate template)
    {
        List<FormInstance> formInstances = new();

        if (form.AssignmentType is AssignmentType.Team)
        {
            formInstances.AddRange(form.SelectedLinkedProjects.Select(CreateTeamFormInstanceWithBlocks));
        }
        else
        {
            foreach (var project in form.SelectedSingleProjects)
            {
                var memberships = await GetRecipients(project.Id, form.SelectedRoles);

                foreach (var membership in memberships)
                    if (form.AssignmentType == AssignmentType.Pairwise)
                        foreach (var pairMembership in memberships.Where(m => m.UserId != membership.UserId))
                            formInstances.Add(CreateFormInstanceWithBlocks(membership, pairMembership));
                    else
                        formInstances.Add(CreateFormInstanceWithBlocks(membership));
            }
        }

        return formInstances;
    }
    
    /// <inheritdoc />
    public async Task<IList<ProjectUserMembership>> GetRecipients(long projectId, IDictionary<ProjectRole, bool> selectedRoles)
    {
        var dbProject = await _projectRepository.GetByIdAsync(projectId, ProjectIncludes.Member);
        var validRoles = selectedRoles.Where(v => v.Value).Select(x => x.Key);
        var memberships = dbProject.MemberAssociations.Where(a => validRoles.Contains(a.Role)).ToList();
        return memberships;
    }

    private static TeamFormInstance CreateTeamFormInstanceWithBlocks(LinkedProjects linkedProjects)
    {
        var newTeamFormInstance = new TeamFormInstance
        {
            ProjectId = linkedProjects.FirstProject.Id,
            LinkedProjectId = linkedProjects.SecondProject.Id
        };
        return newTeamFormInstance;
    }

    private static FormInstance CreateFormInstanceWithBlocks(ProjectUserMembership membership, ProjectUserMembership pairMembership = null)
    {
        var newFormInstance = new UserFormInstance
        {
            AssigneeId = membership.UserId,
            ProjectId = membership.ProjectId
        };
        if (pairMembership is not null) newFormInstance.PairId = pairMembership.UserId;

        return newFormInstance;
    }

    private async Task ThrowIfFormInstanceAlreadySubmitted(long formInstanceId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var formInstanceStatus = await context.FormInstances
            .Where(x => x.Id == formInstanceId)
            .Select(x => x.Status)
            .FirstOrDefaultAsync();
        if (formInstanceStatus is FormStatus.Submitted)
            throw new InvalidOperationException("Cannot save an answer to a form that has already been submitted");
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<UserFormInstance>> GetUserFormsForUserInProject(long userId, long projectId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.UserFormInstances
            .Where(i => i.AssigneeId == userId && i.ProjectId == projectId)
            .Include(i => i.Pair)
            .Include(i => i.Assignment)
            .AsNoTracking()
            .ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<TeamFormInstance>> GetTeamFormsForProject(long projectId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.TeamFormInstances.Where(i => i.ProjectId == projectId)
            .Include(i => i.LinkedProject)
            .Include(i => i.Assignment)
            .AsNoTracking()
            .ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<UserFormInstance> GetUserFormInstanceById(long formInstanceId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.UserFormInstances
            .Where(i => i.Id == formInstanceId)
            .Include(instance => instance.Answers)
                .ThenInclude(a => (a as MultiChoiceAnswer).SelectedOptions)
                .ThenInclude(o => o.MultichoiceOption)
            .Include(instance => instance.Assignment)
                .ThenInclude(a => a.FormTemplate)
                .ThenInclude(t => t.Blocks)
            .Include(i => i.Pair)
            .AsNoTracking()
            .SingleOrDefaultAsync();
    }
    
    /// <inheritdoc />
    public async Task<TeamFormInstance> GetTeamFormInstanceById(long formInstanceId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.TeamFormInstances
            .Where(x => x.Id == formInstanceId)
            .Include(instance => instance.Answers)
                .ThenInclude(a => (a as MultiChoiceAnswer).SelectedOptions)
                .ThenInclude(o => o.MultichoiceOption)
            .Include(instance => instance.Assignment)
                .ThenInclude(a => a.FormTemplate)
                .ThenInclude(t => t.Blocks)
            .Include(x => x.LinkedProject)
            .AsNoTracking()
            .SingleOrDefaultAsync();
    }
    
    public async Task SaveAnswerToTextFormBlock(long formInstanceId, long questionId, string textAnswer, long idOfEditingUser, bool shouldChangesBeBroadcastToWholeProject)
    {
        await ThrowIfFormInstanceAlreadySubmitted(formInstanceId);
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        var answer = await context.TextAnswers.FirstOrDefaultAsync(x => x.FormInstanceId == formInstanceId && x.QuestionId == questionId);
        var isNewAnswer = answer is null;
        var now = _clock.Now;
        
        answer ??= new TextAnswer
        {
            FormInstanceId = formInstanceId,
            QuestionId = questionId,
            Created = now
        };
        answer.Answer = textAnswer;
        answer.LastUpdated = now;
        
        context.TextAnswers.Update(answer);
        await context.SaveChangesAsync();

        var formInstance = await context.FormInstances.FirstAsync(x => x.Id == formInstanceId);
        await BroadCastChangesAfterAnswerUpdate(idOfEditingUser, shouldChangesBeBroadcastToWholeProject, answer, formInstance, isNewAnswer);
    }
    
    public async Task SaveAnswerToMultiChoiceFormBlock(
        long formInstanceId, 
        long questionId, 
        ICollection<long> multiChoiceOptionIds, 
        long idOfEditingUser, 
        bool shouldChangesBeBroadcastToWholeProject
    ) {
        await ThrowIfFormInstanceAlreadySubmitted(formInstanceId);
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var question = await context.MultichoiceQuestions.SingleOrDefaultAsync(x => x.Id == questionId);
        if (question is null) throw new ArgumentException("No such question found with given ID");
        if (!question.AllowMultiple && multiChoiceOptionIds.Count != 1) throw new ActionNotSupportedException("Cannot select multiple options on this question");
        
        var answer = await context.MultichoiceAnswers.FirstOrDefaultAsync(x => x.FormInstanceId == formInstanceId && x.QuestionId == questionId);
        var isNewAnswer = answer is null;
        var now = _clock.Now;
        
        answer ??= new MultiChoiceAnswer
        {
            FormInstanceId = formInstanceId,
            QuestionId = questionId,
            Created = now
        };
        answer.SelectedOptions = multiChoiceOptionIds.Select(optionId => new MultichoiceAnswerMultichoiceOption
        {
            MultichoiceOptionId = optionId,
            MultichoiceAnswerId = answer.Id
        }).ToList();
        answer.LastUpdated = now;
        
        context.MultichoiceAnswers.Update(answer);
        await context.SaveChangesAsync();

        var formInstance = await context.FormInstances.FirstAsync(x => x.Id == formInstanceId);
        await BroadCastChangesAfterAnswerUpdate(idOfEditingUser, shouldChangesBeBroadcastToWholeProject, answer, formInstance, isNewAnswer);
    }

    /// <summary>
    /// Broadcast a new value for this answer, and if there was no existing answer then also broadcast that the state of the parent form instance has changed
    /// </summary>
    private async Task BroadCastChangesAfterAnswerUpdate<TAnswer>(
        long idOfEditingUser, 
        bool shouldChangesBeBroadcastToWholeProject, 
        TAnswer answer, 
        FormInstance formInstance, 
        bool isNewAnswer
    ) where TAnswer : Answer
    {
        if (shouldChangesBeBroadcastToWholeProject)
            await _entityLiveUpdateService.BroadcastNewValueForEntityToProjectAsync(answer.Id, formInstance.ProjectId, answer, idOfEditingUser);
        else
            await _entityLiveUpdateService.BroadcastNewValueForEntityToUserAsync(answer.Id, idOfEditingUser, answer, idOfEditingUser);

        if (isNewAnswer)
        {
            if (shouldChangesBeBroadcastToWholeProject)
                await _entityLiveUpdateService.BroadcastChangeHasOccuredForEntityToProjectAsync<FormInstance>(formInstance.Id, formInstance.ProjectId);
            else
                await _entityLiveUpdateService.BroadcastChangeHasOccuredForEntityToUserAsync<FormInstance>(formInstance.Id, idOfEditingUser);
        }
    }


    public async Task SubmitForm(long formInstanceId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var formInstance = await context.FormInstances.FirstOrDefaultAsync(x => x.Id == formInstanceId);

        if (formInstance is null) throw new ArgumentException("No such form instance found with given ID");

        formInstance.Status = FormStatus.Submitted;
        formInstance.SubmittedDate = _clock.Now;
        context.Update(formInstance);
        await context.SaveChangesAsync();
    }
    
    /// <inheritdoc />
    public async Task<PaginatedList<Assignment>> GetPaginatedAssignments(long formTemplateId, int pageIndex)
    {
        AssignmentTransformType filter = query =>
            query.Where(assignment => assignment.FormTemplateId == formTemplateId)
                .OrderByDescending(assignment => assignment.RunNumber)
                .ThenBy(assignment => assignment.Id);
        return await _assignmentRepository.GetAllPaginatedAsync(pageIndex, PageSize, filter, AssignmentIncludes.InstancesProjectsAndMembers);
    }

    public async Task<Answer> GetAnswerByFormInstanceAndQuestionIdAsync(long formInstanceId, long questionId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Answers
            .Include(x => (x as MultiChoiceAnswer).SelectedOptions)
            .ThenInclude(o => o.MultichoiceOption)
            .FirstOrDefaultAsync(x => x.FormInstanceId == formInstanceId && x.QuestionId == questionId);
    }

    public async Task<Assignment> GetAssignmentByIdAsync(long assignmentId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Assignments
            .FirstOrDefaultAsync(x => x.Id == assignmentId);
    }

    public async Task<ICollection<FormInstance>> GetFormInstancesForProjectAndAssignmentAsync(long projectId, long assignmentId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.FormInstances
            .Where(x => x.AssignmentId == assignmentId)
            .Where(x => x.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<ICollection<Assignment>> GetAllAssignmentsForProjectAsync(long projectId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.FormInstances
            .Where(x => x.ProjectId == projectId)
            .Select(x => x.Assignment)
            .Distinct()
            .Include(x => x.FormTemplate)
            .Include(x => x.Instances)
            .ToListAsync();
    }
}