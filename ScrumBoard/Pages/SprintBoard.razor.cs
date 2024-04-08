using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Repositories;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Shared;

namespace ScrumBoard.Pages;

public partial class SprintBoard : BaseProjectScopedComponent
{
    [Inject]
    public IUserStoryRepository UserStoryRepository { get; set; }

    private bool FilterEnabled => _assigneeFilter.Any() || _reviewerFilter.Any();

    public Sprint Sprint;

    private ICollection<UserStory> _stories = new List<UserStory>();

    private StoryTaskDisplayPanel _storyTaskPanel;

    private ICollection<User> _reviewerFilter = new List<User>();
    private ICollection<User> _assigneeFilter = new List<User>();
    private ICollection<User> _members;

    private async Task OnStoryUpdated(UserStory story)
    {
        var storyIndex = _stories
            .Select((s, index) => new { Story=s, Index=index})
            .FirstOrDefault(i => i.Story.Id == story.Id)?.Index;
        if (!storyIndex.HasValue) {
            Logger.LogWarning("Updated unknown story on sprintboard {StoryId}", story.Id);
            await UpdateStories();
            return;
        }

        var foundStory = await UserStoryRepository.GetByIdAsync(story.Id);
        if (foundStory == null) {
            Logger.LogWarning("Updated story on sprintboard, but could not retrieve story {StoryId}", story.Id);
            await UpdateStories();
            return;
        }
        _stories.ToArray()[storyIndex.Value] = foundStory;           
        NotifyStateChange();
    }

    private async Task UpdateStories()
    {
        if (Sprint == null) {
            _stories = new List<UserStory>();
        } else {
            _stories = await UserStoryRepository.GetByStoryGroupAsync(Sprint);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await UpdateProject();
    }

    private bool TaskFilter(UserStoryTask task)
    {
        if (!_reviewerFilter.Any() && !_assigneeFilter.Any()) return true;
            
        if (_reviewerFilter.Any() && _reviewerFilter.Select(user => user.Id).Any(task.GetReviewingUsers().Select(user => user.Id).Contains)) {
            return true;
        }
            
        if (_assigneeFilter.Any() && _assigneeFilter.Select(user => user.Id).Any(task.GetAssignedUsers().Select(user => user.Id).Contains)) {
            return true;
        }
        return false;
    }


    private void ClearFilters()
    {
        _reviewerFilter.Clear();
        _assigneeFilter.Clear();
    }

    private async Task UpdateProject()
    {
        _members = Project.GetWorkingMembers().ToList();
        Sprint = Project.Sprints.FirstOrDefault(sprint => sprint.TimeStarted.HasValue);
        await UpdateStories();
        ClearFilters();
    }

    // Wrapper for StateHasChanged so it can be overridden by integration test
    protected virtual void NotifyStateChange()
    {
        StateHasChanged();
    }
}