using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Services;
using ScrumBoard.Shared;
using ScrumBoard.Utils;

namespace ScrumBoard.Pages;

public partial class ViewFormResponses : BaseProjectScopedComponent
{
    [Inject] protected IFormInstanceService FormInstanceService { get; set; }
    [Inject] protected IFormTemplateService FormTemplateService { get; set; }
    [Inject] protected IUserService UserService { get; set; }
    [Inject] protected IClock Clock { get; set; }
    
    [Parameter, EditorRequired] public long AssignmentId { get; set; }
    
    private bool _forbidden;
    private Assignment _assignment;
    private ICollection<FormInstance> _formInstances = [];
    private ICollection<FormTemplateBlock> _orderedTemplateBlocks;
    private List<User> _usersInAssigment = [];
    private User _filteredAssignee;
    private User _filteredPair;

    private IEnumerable<FormInstance> GetCurrentlyViewedFormInstances()
    {
        if (_assignment.AssignmentType is AssignmentType.Team)
        {
            return _formInstances.OrderBy(x => ((TeamFormInstance)x).Project.Name);
        }

        return _formInstances
            .Where(x => _filteredAssignee is null || _filteredAssignee.Id == ((UserFormInstance)x).AssigneeId)
            .Where(x => _filteredPair is null || _filteredPair.Id == ((UserFormInstance)x).PairId)
            .OrderBy(x => ((UserFormInstance)x).Assignee.GetFullName())
            .ThenBy(x => ((UserFormInstance)x).Pair?.GetFullName());
    }
    
    private DateTime GetSubmissionDateOrNow(FormInstance formInstance) => formInstance.Status is FormStatus.Submitted ? formInstance.SubmittedDate ?? Clock.Now : Clock.Now;
    private TimeSpan GetTimeSinceSubmission(FormInstance formInstance) => GetSubmissionDateOrNow(formInstance) - formInstance.Assignment.EndDate;
    private bool IsOverdue(FormInstance formInstance) => GetSubmissionDateOrNow(formInstance) > formInstance.Assignment.EndDate;
    private string TimeFromSubmissionString(FormInstance formInstance) => DurationUtils.DurationStringFrom(
        GetTimeSinceSubmission(formInstance),
        DurationFormatOptions.AlwaysShowAsPositiveValue
        | DurationFormatOptions.TakeTwoHighestUnitsOnly
        | DurationFormatOptions.UseDaysAsLargestUnit
    );
    

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (RoleInCurrentProject is not ProjectRole.Leader)
        {
            Logger.LogWarning(
                "User with ID={UserId} tried to view forms for a project they are not a leader of (Project ID={ProjectId})",
                Self.Id,
                Project.Id
            );
            _forbidden = true;
            return;
        }
        
        if(AssignmentId == default || (_assignment is not null && AssignmentId == _assignment.Id)) return;
        _assignment = await FormInstanceService.GetAssignmentByIdAsync(AssignmentId);
        _orderedTemplateBlocks = await FormTemplateService.GetOrderedBlocksForFormTemplateAsync(_assignment.FormTemplateId);
        
        _formInstances = await FormInstanceService.GetFormInstancesForProjectAndAssignmentAsync(Project.Id, AssignmentId);
        if (_assignment.AssignmentType is AssignmentType.Individual or AssignmentType.Pairwise)
        {
            var userIds = _formInstances.Select(x => ((UserFormInstance)x).AssigneeId);
            _usersInAssigment = (await UserService.GetUsersByIdsAsync(userIds)).OrderBy(x => x.GetFullName()).ToList();
        }
    }
}