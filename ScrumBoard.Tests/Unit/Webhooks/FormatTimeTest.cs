using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Unit.Webhooks
{
    public class FormatTimeTest
    {        
        private TimeSpan expectedTime;

        [Theory]
        [InlineData("#task T1 #time 1h 3m 1s this is my message", 2, 3781)]
        [InlineData("#task T1 #time 1s this is my message", 2, 1)]
        [InlineData("#task T1 #time 3m 1s this is my message", 2, 181)]
        [InlineData("#task T1 #time 3m this is my message", 2, 180)]
        [InlineData("#task T1 #time 1h 3m 1s #pair abc123,abc234 #fix this is my message", 2, 3781)]
        [InlineData("#task T1 #time 1h 3m 1s #test #pair abc123,abc234 this is my message", 2, 3781)]
        [InlineData("#time 1h 3m 1s #task T1 #test #pair abc123,abc234 this is my message", 0, 3781)]
        [InlineData("#time 1h 3m #task T1 #test #pair abc123,abc234 this is my message", 0, 3780)]
        [InlineData("#task T1 #test #pair abc123,abc234 this is my message #time 1h 3m", 9, 3780)]
        public void FormatTime_ValidTimeString_CorrectDurationReturned(string commitMessage, int splitMessageIndex, int expectedTimeInSeconds)
        {
            expectedTime = TimeSpan.FromSeconds(expectedTimeInSeconds);
            List<string> splitMessage = GitlabCommitUtils.SplitCommitMessage(commitMessage, true);      
            var (result, timeSegmentCount) = GitlabCommitUtils.FormatTime(splitMessageIndex, splitMessage);
            result.Should().Be(expectedTime);
        }

        [Theory]     
        [InlineData("#time #pair abc123,abc234 #fix this is my message", 2)]
        [InlineData("#time #task T1 #test #pair abc123,abc234 this is my message", 0)]
        [InlineData("#time -1h #task T1 #test #pair abc123,abc234 this is my message", 0)]
        [InlineData("#time -1h -1m -1s #task T1 #test #pair abc123,abc234 this is my message", 0)]
        [InlineData("#time -1h -1s #task T1 #test #pair abc123,abc234 this is my message", 0)]
        [InlineData("#time -1s #task T1 #test #pair abc123,abc234 this is my message", 0)]
        [InlineData("#task T1 #test #pair abc123,abc234 this is my message #time 90", 9)]
        public void FormatTime_InValidTimeString_NoDurationReturned(string commitMessage, int splitMessageIndex)
        {
            List<string> splitMessage = GitlabCommitUtils.SplitCommitMessage(commitMessage, true);  
            var (result, timeSegmentCount) = GitlabCommitUtils.FormatTime(splitMessageIndex, splitMessage);
            result.Should().BeNull();
        }
       
    }
}