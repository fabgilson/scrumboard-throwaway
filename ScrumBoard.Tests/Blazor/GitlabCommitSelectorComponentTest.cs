using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Gitlab;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Shared.Widgets;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class GitlabCommitSelectorTest : TestContext
    {
        private IRenderedComponent<GitlabCommitSelector> _component;

        private User _actingUser;

        private GitlabCommit _commitOne;
        private GitlabCommit _commitTwo;
        private GitlabCommit _commitThree;

        private GitlabCredentials _gitlabCredentials = new();
        private GitlabBranch _branchOne;
        private GitlabBranch _branchTwo;
        private List<GitlabBranch> _branches;
        private List<GitlabCommit> _commits; 
        private List<GitlabCommit> _branchOneCommits;

        // Mocks
        private readonly Mock<IGitlabService> _mockGitlabService = new();
        private readonly Mock<IGitlabCommitRepository> _mockGitlabCommitRepository = new();
        private readonly Mock<IJsInteropService> _mockJsInteropService = new();
        private readonly Mock<IUserFlagService> _mockUserFlagService = new();

        private void CreateComponent(bool withCredentials = true)
        {
            Services.AddScoped(_ => _mockGitlabService.Object);
            Services.AddScoped(_ => _mockGitlabCommitRepository.Object);
            Services.AddScoped(_ => _mockJsInteropService.Object);
            Services.AddScoped(_ => _mockUserFlagService.Object);

            _actingUser = new User { Id = 500, FirstName = "John", LastName = "Smith", LDAPUsername = "jhs101" };

            _branchOne = new GitlabBranch { Name = "Branch One" };
            _branchTwo = new GitlabBranch { Name = "Branch Two" };

            _branches = new List<GitlabBranch> { _branchOne, _branchTwo };

            _commitOne = new GitlabCommit { Id = "commit1", Message = "Commit One", AuthorName = "jhs101", AuthorEmail = "jhs101@email.com" };
            _commitTwo = new GitlabCommit { Id = "commit2", Message = "Commit Two #pair jhs101,abc123", AuthorName = "abc123", AuthorEmail = "abc123@email.com" };
            _commitThree = new GitlabCommit { Id = "commit3", Message = "Commit Three", AuthorName = "abc123", AuthorEmail = "abc123@email.com"};

            _branchOneCommits = new List<GitlabCommit> 
            {
                _commitOne,
                _commitTwo,
                _commitThree
            };

            _commits = new List<GitlabCommit> { _commitOne };
            
            if (!withCredentials)
            {
                _gitlabCredentials = null;
            }

            _mockGitlabService
                .Setup(mock => mock.GetBranches(_gitlabCredentials, It.IsAny<string[]>()))
                .ReturnsAsync(_branches);
            _mockGitlabService.
                Setup(mock => mock.GetCommits(_gitlabCredentials, It.IsAny<string[]>()))
                .ReturnsAsync(_branchOneCommits);
            _mockGitlabCommitRepository
                .Setup(mock => mock.HasLinkAsync(_commitOne, _actingUser))
                .ReturnsAsync(true);

            _component = RenderComponent<GitlabCommitSelector>(parameters => parameters
                .Add(cut => cut.WorklogCommits, _commits)
                .Add(cut => cut.WorklogCommitsChanged, (commits) => { _commits = commits.ToList();})
                .Add(cut => cut.Credentials, _gitlabCredentials)
                .Add(cut => cut.Self, _actingUser)
            );
        }

        [Fact]
        public void DefaultState_NoAction_DisplaysLinkedCommits()
        {
            CreateComponent();
            _component.WaitForAssertion(() => _component.FindAll(".worklog-commit").Should().HaveCount(1));
        }

        [Fact]
        public void DefaultState_LinkCommitsClicked_DisplaysBranches()
        {
            CreateComponent();
            _component.Find("#link-commits-button").Click();
            _component.WaitForAssertion(() => _component.FindAll(".branch").Should().HaveCount(_branches.Count));
        }
        
        [Fact]
        public void InBranches_LinkCommitsClicked_DisplaysUnlinkedCommits()
        {
            CreateComponent();
            _component.Find("#link-commits-button").Click();
            _component.Find("#branch-0").Click();
            
            _component.Find("#view-all-commits-checkbox").Change(false);

            var commits = _component.FindAll(".commit");
            commits.Should().HaveCount(1);
        }

        [Fact]
        public void InCommits_AllCommitsClicked_DisplaysAllCommits()
        {
            CreateComponent();
            _component.Find("#link-commits-button").Click();
            _component.Find("#branch-0").Click();

            _component.Find("#view-all-commits-checkbox").Change(true);
            _component.WaitForAssertion(() => _component.FindAll(".commit").Should().HaveCount(2));
        }
        
        [Fact]
        public void InCommits_BackButtonClicked_DisplaysBranches()
        {
            CreateComponent();
            _component.Find("#link-commits-button").Click();
            _component.Find("#branch-0").Click();
            
            _component.Find("#back-to-branches-button").Click();
            _component.WaitForState(() => !_component.FindAll(".commit").Any());
            _component.WaitForAssertion(() => _component.FindAll(".branch").Should().NotBeEmpty());
        }


        [Fact]
        public void InBranches_BackButtonClicked_GoesBackToWorklogCommits()
        {
            CreateComponent();
            _component.Find("#link-commits-button").Click();
            
            _component.Find("#back-to-worklog-commits-button").Click();
            _component.WaitForState(() => !_component.FindAll(".branch").Any());
            _component.WaitForAssertion(() => _component.FindAll(".worklog-commit").Should().NotBeEmpty());
        }

        [Fact]
        public void AddingByHash_AddWithShortHash_AddsCommit()
        {
            CreateComponent();
            var initialNumCommits = _commits.Count;
            const string commitTwoHash = "120f31e3";

            _mockGitlabService.Setup(x => x.GetCommit(_gitlabCredentials, commitTwoHash)).ReturnsAsync(_commitTwo);
            _component.Find("#hash-input").Change(commitTwoHash);
            _component.Find("#add-hash-button").Click();
            _commits.Count.Should().Be(initialNumCommits + 1);
        }
        
        [Fact]
        public void AddingByHash_AddWithLongHash_AddsCommit()
        {
            CreateComponent();
            var initialNumCommits = _commits.Count;
            const string commitTwoHash = "4709b711d01a166f9cdcf34db72dce5928dc27f2";

            _mockGitlabService.Setup(x => x.GetCommit(_gitlabCredentials, commitTwoHash)).ReturnsAsync(_commitTwo);
            _component.Find("#hash-input").Change(commitTwoHash);
            _component.Find("#add-hash-button").Click();
            _commits.Count.Should().Be(initialNumCommits + 1);
        }
        
        [Fact]
        public void AddingByHash_AttemptAddDuplicate_CountUnchanged()
        {
            CreateComponent();
            const string commitTwoHash = "120f31e3";
            _mockGitlabService.Setup(x => x.GetCommit(_gitlabCredentials, commitTwoHash)).ReturnsAsync(_commitTwo);
            
            // Add commitTwo for the first time
            _component.Find("#hash-input").Change(commitTwoHash);
            _component.Find("#add-hash-button").Click();
            
            var initialNumCommits = _commits.Count;
            
            // Try adding commitTwo again
            _component.Find("#hash-input").Change(commitTwoHash);
            _component.Find("#add-hash-button").Click();
            
            _commits.Count.Should().Be(initialNumCommits);
        }

        [Fact]
        public void AddingByHash_AttemptAddCommitInOtherWorkLog_CountUnchanged()
        {
            CreateComponent();
            var initialNumCommits = _commits.Count;
            const string commitThreeHash = "120f31e4";
            
            // Make it as if commit three was attached to some other work log
            _mockGitlabCommitRepository
                .Setup(mock => mock.HasLinkAsync(_commitThree, _actingUser))
                .ReturnsAsync(true);
            
            _mockGitlabService.Setup(x => x.GetCommit(_gitlabCredentials, commitThreeHash)).ReturnsAsync(_commitThree);
            
            _component.Find("#hash-input").Change(commitThreeHash);
            _component.Find("#add-hash-button").Click();
            
            _commits.Count.Should().Be(initialNumCommits);
        }
        
        public static IEnumerable<object[]> RequestFailureEnumValues()
        {
            return from object reason in Enum.GetValues(typeof(RequestFailure)) select new object[] { reason };
        }
        
        [Theory]
        [MemberData(nameof(RequestFailureEnumValues))]
        public void AddingByHash_GitlabException_ErrorDisplayed(RequestFailure failureReason)
        {
            CreateComponent();
            const string commitTwoHash = "120f31e3";

            _mockGitlabService.Setup(x => x.GetCommit(_gitlabCredentials, commitTwoHash))
                .Throws(new GitlabRequestFailedException(failureReason));
            _component.Find("#hash-input").Change(commitTwoHash);
            _component.Find("#add-hash-button").Click();

            _component.FindAll("#hash-error-message").Count.Should().Be(1);
        }

        [Fact]
        public void AddingByHash_BlankHash_NoEffect()
        {
            CreateComponent();
            var initialNumCommits = _commits.Count;
            const string commitTwoHash = "120f31e3";

            _mockGitlabService.Setup(x => x.GetCommit(_gitlabCredentials, commitTwoHash)).ReturnsAsync(_commitTwo);
            _component.Find("#hash-input").Change("");
            _component.Find("#add-hash-button").Click();

            _commits.Count.Should().Be(initialNumCommits);
        }

        [Fact]
        public void DefaultState_NoGitCredentials_CommitSearchIsNotShown()
        {
            CreateComponent(false);
            _component.FindAll("#worklog-commit-search").Should().BeEmpty();
        }
        
        [Fact]
        public void DefaultState_GitCredentialsExist_CommitSearchIsShown()
        {
            CreateComponent();
            _component.FindAll("#worklog-commit-search").Should().ContainSingle();
        }
        
        [Fact]
        public void DefaultState_NoGitCredentials_FindCommitSectionIsNotShown()
        {
            CreateComponent(false);
            _component.FindAll("#find-commit-section").Should().BeEmpty();
        }
        
        [Fact]
        public void DefaultState_GitCredentialsExist_FindCommitSectionShown()
        {
            CreateComponent();
            _component.FindAll("#find-commit-section").Should().ContainSingle();
        }

        [Fact]
        public void ToggleAllCommitsOn_AllCommitsCheckboxChecked()
        { 
            _mockUserFlagService
                .Setup(x => x.IsFlagSetForUserAsync(It.IsAny<long>(), It.IsAny<SinglePerUserFlagType>()))
                .ReturnsAsync(true);
            CreateComponent();
            _component.Find("#link-commits-button").Click();
            _component.Find("#branch-0").Click();
            var viewAllCheckBox = _component.Find("#view-all-commits-checkbox");
            viewAllCheckBox.HasAttribute("checked").Should().BeTrue();
        }
        
        [Fact]
        public void ToggleAllCommitsOff_AllCommitsCheckboxNotChecked()
        { 
            _mockUserFlagService
                .Setup(x => x.IsFlagSetForUserAsync(It.IsAny<long>(), It.IsAny<SinglePerUserFlagType>()))
                .ReturnsAsync(false);
            CreateComponent();
            _component.Find("#link-commits-button").Click();
            _component.Find("#branch-0").Click();
            var viewAllCheckBox = _component.Find("#view-all-commits-checkbox");
            viewAllCheckBox.HasAttribute("checked").Should().BeFalse();
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToggleViewAllCommitsCheckbox_DifferentInitialStates_ServiceLayerCalledCorrectly(bool initialState)
        { 
            _mockUserFlagService
                .Setup(x => x.IsFlagSetForUserAsync(It.IsAny<long>(), It.IsAny<SinglePerUserFlagType>()))
                .ReturnsAsync(initialState);
            CreateComponent();
            _component.Find("#link-commits-button").Click();
            _component.Find("#branch-0").Click();
            var viewAllCheckBox = _component.Find("#view-all-commits-checkbox");
            viewAllCheckBox.Change(!initialState);
            
            _mockUserFlagService.Verify(x => x.SetFlagForUserAsync(_actingUser.Id, SinglePerUserFlagType.ViewAllCommits, !initialState));
        }
    }
}