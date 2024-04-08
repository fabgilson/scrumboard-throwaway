using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Unit.Webhooks
{
    public class ParseCommitTest
    {
        private readonly List<WorklogTag> _validTags = new() {
            new() { Name = "Test"},
            new() { Name = "Document"},
            new() { Name = "Fix"},
            new() { Name = "Chore"},
            new() { Name = "Feature"},
            new() { Name = "Refactor"},
        };

        [Fact]
        public void DescriptionHasNoTags_EntireMessageReturned() {
            string commitMessage = "This is a test commit message";        
            var (commitTags, worklogTags, description) = GitlabCommitUtils.ParseCommitMessage(commitMessage, _validTags);
            description.Should().Be(commitMessage);
        }

        [Theory]
        [InlineData("#task T1 #time 3h 1m 10s This is a commit message", "This is a commit message")]
        [InlineData("#task T1 #time 3h 10s This is a commit message", "This is a commit message")]
        [InlineData("#task T1 #time 1m 10s This is a commit message", "This is a commit message")]
        [InlineData("#task T1 #time 10s This is a commit message", "This is a commit message")]
        [InlineData("#fix #document This is a commit message", "This is a commit message")]
        [InlineData("#pair usr123,usr124 This is a commit message", "This is a commit message")] 
        [InlineData("#pair usr123,usr124 #fix This is a commit message", "This is a commit message")]  
        [InlineData("#time 3h 1m 10s #pair usr123,usr124 #fix This is a commit message", "This is a commit message")]
        [InlineData("#pair usr123,usr124 #time 3h 1m 10s #fix\r\nThis is a commit message", "This is a commit message")]  
        [InlineData("#pair usr123,usr124 #time 3h 1m 10s #fix\rThis is a commit message", "This is a commit message")] 
        [InlineData("#pair usr123,usr124 #time 3h 1m 10s #fix\nThis is a commit message", "This is a commit message")] 
        public void DescriptionHasTagsAtBeginning_MessageReturnedCorrectly(string commitMessage, string expectedDescription) {               
            var (commitTags, worklogTags, description) = GitlabCommitUtils.ParseCommitMessage(commitMessage, _validTags);
            description.Should().Be(expectedDescription);
        }

        [Theory]
        [InlineData("This is a commit message #task T1 #time 3h 1m 10s", "This is a commit message")]
        [InlineData("This is a commit message #task T1 #time 3h 10s", "This is a commit message")]
        [InlineData("This is a commit message #task T1 #time 1m 10s", "This is a commit message")]
        [InlineData("This is a commit message #task T1 #time 10s", "This is a commit message")]
        [InlineData("This is a commit message #fix #document #chore", "This is a commit message")]
        [InlineData("This is a commit message #pair usr123,usr124", "This is a commit message")]  
        [InlineData("This is a commit message #pair usr123,usr124 #fix", "This is a commit message")]  
        [InlineData("This is a commit message #time 3h 1m 10s #pair usr123,usr124 #fix", "This is a commit message")]
        [InlineData("This is a commit message #pair usr123,usr124 #time 3h 1m 10s #fix", "This is a commit message")]
        [InlineData("This is a commit message\r\n#pair usr123,usr124 #time 3h 1m 10s #fix", "This is a commit message")]
        [InlineData("This is a commit message\n#pair usr123,usr124 #time 3h 1m 10s #fix", "This is a commit message")]
        [InlineData("This is a commit message\r#pair usr123,usr124 #time 3h 1m 10s #fix", "This is a commit message")]
        [InlineData("This is a commit message #task T1 #time 1h #time 3h 30m", "This is a commit message")]
        public void DescriptionHasTagsAtEnd_MessageReturnedCorrectly(string commitMessage, string expectedDescription) {              
            var (commitTags, worklogTags, description) = GitlabCommitUtils.ParseCommitMessage(commitMessage, _validTags);
            description.Should().Be(expectedDescription);
        }

        [Theory]
        [InlineData("This is a commit message #task T1 #time 3h 1m 10s And here is more", "This is a commit message And here is more")]
        [InlineData("This is a commit message #task T1 #time 3h 10s And here is more", "This is a commit message And here is more")]
        [InlineData("This is a commit message #task T1 #time 1m 10s And here is more", "This is a commit message And here is more")]
        [InlineData("This is a commit message #task T1 #time 10s And here is more", "This is a commit message And here is more")]
        [InlineData("This is a commit message #fix #document #chore And here is more", "This is a commit message And here is more")]
        [InlineData("This is a commit message #pair usr123,usr124 And here is more", "This is a commit message And here is more")]
        [InlineData("This is a commit message #pair usr123,usr124 #fix And here is more", "This is a commit message And here is more")]  
        [InlineData("This is a commit message #time 3h 1m 10s #pair usr123,usr124 #fix And here is more", "This is a commit message And here is more")]
        [InlineData("This is a commit message #pair usr123,usr124 #time 3h 1m 10s #fix And here is more", "This is a commit message And here is more")]
        [InlineData("This is a commit message #time 3h 1m 10s\r\n#pair usr123,usr124 #fix And here is more", "This is a commit message\nAnd here is more")]
        [InlineData("This is a commit message #pair usr123,usr124\n#time 3h 1m 10s #fix And here is more", "This is a commit message\nAnd here is more")]
        [InlineData("This is a commit message #pair usr123,usr124\r#time 3h 1m 10s #fix And here is more", "This is a commit message\nAnd here is more")]
        [InlineData("This is a commit message #task T1 #time 3h 1m 10s\r\n#pair usr123,usr124 #fix And here is more", "This is a commit message\nAnd here is more")]
        public void DescriptionHasTagsBetweenText_MessageReturnedCorrectly(string commitMessage, string expectedDescription) {              
            var (commitTags, worklogTags, description) = GitlabCommitUtils.ParseCommitMessage(commitMessage, _validTags);            
            description.Should().Be(expectedDescription);
        }

       
    }
}