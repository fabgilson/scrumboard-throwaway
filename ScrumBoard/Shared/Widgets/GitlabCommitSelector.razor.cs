using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Gitlab;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Utils;
using ScrumBoard.Models.Forms;

namespace ScrumBoard.Shared.Widgets
{
    public partial class GitlabCommitSelector
    {
        [CascadingParameter(Name = "Self")]
        public User Self { get; set; }
        
        [Inject]
        protected IGitlabService GitlabService { get; set; }

        [Inject]
        protected ILogger<GitlabCommitSelector> Logger { get; set; }
        
        [Inject]
        protected IGitlabCommitRepository GitlabCommitRepository { get; set; }
        
        [Inject]
        protected IJsInteropService JsInteropService { get; set; }
        
        [Inject]
        protected IUserFlagService UserFlagService { get; set; }

        [Parameter]
        public GitlabCredentials Credentials { get; set; }

        [Parameter]
        public IEnumerable<GitlabCommit> WorklogCommits { get; set; }

        [Parameter]
        public EventCallback<ICollection<GitlabCommit>> WorklogCommitsChanged { get; set; }

        private List<GitlabCommit> _currentCommits;

        private IEnumerable<GitlabBranch> _branches = new List<GitlabBranch>();

        private ElementReference _dropdown;

        private string _commitHash;

        private IEnumerable<GitlabBranch> FilteredBranches
        {
            get
            {
                var query = _branchSearchQuery.Trim();
                return _branches.Where(branch => branch.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            }
        }

        private IEnumerable<GitlabCommit> FilteredCommits
        {
            get
            {
                var query = _commitSearchQuery.Trim();
                return FilterCommits(query, _commits);
            }
        }

        private IEnumerable<GitlabCommit> FilteredWorklogCommits
        {
            get
            {
                var query = _worklogCommitSearchQuery.Trim();
                return FilterCommits(query, WorklogCommits);
            }
        }

        private IEnumerable<GitlabCommit> FilterCommits(string query, IEnumerable<GitlabCommit> commits)
        {
            return commits.Where(commit => 
                    commit.Message.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    commit.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    commit.AuthorName.Contains(query, StringComparison.OrdinalIgnoreCase)
                );
        }

        private IEnumerable<GitlabCommit> _commits = new List<GitlabCommit>();

        private GitlabBranch _selectedBranch;

        private bool _viewingCommits = true;

        private bool CannotLink => _currentCommits.SequenceEqual(WorklogCommits);

        private bool _networkError = false;

        private string _branchSearchQuery = "";

        private string _commitSearchQuery = "";

        private string _worklogCommitSearchQuery = "";

        private bool _showErrorMessage = false;

        private string _errorMessage = "";

        private bool _viewingAllCommits = false;

        private int _page = 1;

        private bool? _initialCheckBoxValue;

        protected override void OnInitialized()
        {
            _currentCommits = WorklogCommits.ToList();
            base.OnInitialized();
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            _initialCheckBoxValue = await UserFlagService.IsFlagSetForUserAsync(Self.Id, SinglePerUserFlagType.ViewAllCommits);
        }

        private async Task GetBranches()
        {
            _networkError = false;
            try
            {
                _branches = await GitlabService.GetBranches
                (
                    Credentials,
                    GitlabApiAttribute.PerPage(100)
                );
            }
            catch (GitlabRequestFailedException ex)
            {
                _networkError = true;
                Logger.LogError(ex, "Failed to request branches");
            }
            _viewingCommits = false;
        }

        private async Task GetCommits(GitlabBranch branch, bool allCommits, int page = 1)
        {
            var twoWeeksAgo = DateTime.Now.AddDays(-14);
            _networkError = false;
            try
            {
                _commits = await GitlabService.GetCommits(
                    Credentials,
                    GitlabApiAttribute.RefName(branch.Name),
                    GitlabApiAttribute.Since(twoWeeksAgo), 
                    GitlabApiAttribute.Page(page)
                );
            }
            catch (GitlabRequestFailedException ex)
            {
                Logger.LogError(ex, "Failed to fetch commits");
                _networkError = true;
            }

            if (!allCommits)
            {
                _commits = _commits.Where(commit => commit.AuthorEmail == Self.Email || NameInPairTag(commit));
            }
            
            
            _commits = _commits.Where(commit =>
                WorklogCommits.All(c => c.Id != commit.Id)
            ).ToList();
            
            var areLinkedToSelf = await Task.WhenAll(_commits
                .Select(commit => GitlabCommitRepository.HasLinkAsync(commit, Self)));

            _commits = _commits
                .Zip(areLinkedToSelf)
                .Where(pair => !pair.Second)
                .Select(pair => pair.First)
                .ToList();


            _selectedBranch = branch;
            _page = page;
            StateHasChanged();
        }

        /// <summary>
        /// Gets a single commit from GitlabService by the commit hash, then adds links 
        /// commit to the worklog.
        /// </summary>
        /// <returns>Task to be returned</returns>
        private async Task AddCommit()
        {
            _showErrorMessage = false;

            if (_commitHash == "") return;

            try
            {
                var commit = await GitlabService.GetCommit(Credentials, _commitHash);
                var hasLink = await GitlabCommitRepository.HasLinkAsync(commit, Self);

                if (_currentCommits.Any(c => c.Id == commit.Id))
                {
                    _errorMessage = "Commit has already been added to this list";
                    _showErrorMessage = true;
                } else if (hasLink)
                {
                    _errorMessage = "Commit is already linked in a different worklog";
                    _showErrorMessage = true;
                }
                else
                {
                    _currentCommits.Add(commit);
                    await WorklogCommitsChanged.InvokeAsync(_currentCommits);
                    _commitHash = ""; // Clear the commit hash input box so that another hash can be easily input
                    StateHasChanged();
                }
                
            }
            catch (GitlabRequestFailedException ex)
            {
                string errorMessage;
                switch (ex.FailureType)
                {
                    case RequestFailure.NotFound:
                        errorMessage = "The commit was not found. Please try a different hash.";
                        break;
                    case RequestFailure.InvalidPayload:
                        errorMessage = "That hash is invalid.";
                        break;
                    case RequestFailure.ConnectionFailed:
                    case RequestFailure.Forbidden:
                    case RequestFailure.Unauthorized:
                    case RequestFailure.BadHttpStatus:
                    default:
                        errorMessage = "It looks like something is wrong with your project's git configuration. " +
                                       "Please reload the page, and try again.";
                        Logger.LogError(ex, "Failed to get commit from server");
                        break;
                }

                _errorMessage = errorMessage;
                _showErrorMessage = true;
                _networkError = true;
            }
        }

        private bool NameInPairTag(GitlabCommit commit)
        {
            var pairTag = GitlabCommitUtils.ParseCommitMessage(commit.Message, new()).Item1.PairTag;
            return pairTag != null && pairTag.Contains(Self.LDAPUsername);
        }

        private void CheckboxClicked(GitlabCommit commit, ChangeEventArgs args)
        {
            if ((bool) args.Value)
            {
                if (!_currentCommits.Any(c => c.Id == commit.Id))
                {
                    _currentCommits.Add(commit);
                }
            }
            else if (_currentCommits.Any(c => c.Id == commit.Id))
            {
                _currentCommits.Remove(commit);
            }
        }

        private async Task ViewAllCommitsClicked(ChangeEventArgs args)
        {
            _viewingAllCommits = (bool) args.Value;
            _initialCheckBoxValue = _viewingAllCommits;
            await UserFlagService.SetFlagForUserAsync(Self.Id, SinglePerUserFlagType.ViewAllCommits, _viewingAllCommits);
            GetCommits(_selectedBranch, _viewingAllCommits);
        }

        private void Cancel()
        {
            _currentCommits = WorklogCommits.ToList();
            _selectedBranch = null;
            _viewingCommits = true;
        }

        private async Task CloseSelector()
        {
            _selectedBranch = null;
            _viewingCommits = true;
            _currentCommits = WorklogCommits.ToList();
            await JsInteropService.ToggleCommitDropdown();
            StateHasChanged();
        }

        private async Task LinkCommits()
        {
            await WorklogCommitsChanged.InvokeAsync(_currentCommits);
            _selectedBranch = null;
            _viewingCommits = true;
        }

        private async Task RemoveCommit(GitlabCommit commit)
        {
            _currentCommits.Remove(commit);
            await WorklogCommitsChanged.InvokeAsync(_currentCommits);
        }
    }
}