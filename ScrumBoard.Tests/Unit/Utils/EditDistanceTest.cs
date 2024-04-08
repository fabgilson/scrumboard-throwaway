using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Unit.Utils
{
    public class EditDistanceTest
    {
        [Fact]
        public void LongestCommonSubsequence_BothSourceAndDestEmpty_EmptyListReturned()
        {
            EditDistance.LongestCommonSubsequence("","").Should().BeEmpty();
        }
        
        [Fact]
        public void LongestCommonSubsequence_AddedContent_ListWithAdditionOnly()
        {
            var content = "new";
            var segment = EditDistance.LongestCommonSubsequence("",content).Should().ContainSingle().Which;
            segment.Type.Should().Be(EditSegmentType.Added);
            segment.Content.Should().Be(content);
        }
        
        [Fact]
        public void LongestCommonSubsequence_RemovedContent_ListWithRemovalOnly()
        {
            var content = "gone";
            var segment = EditDistance.LongestCommonSubsequence(content, "").Should().ContainSingle().Which;
            segment.Type.Should().Be(EditSegmentType.Removed);
            segment.Content.Should().Be(content);
        }
        
        [Fact]
        public void LongestCommonSubsequence_ContentSame_ListWithUnchangedOnly()
        {
            var content = "same";
            var segment = EditDistance.LongestCommonSubsequence(content, content).Should().ContainSingle().Which;
            segment.Type.Should().Be(EditSegmentType.Unchanged);
            segment.Content.Should().Be(content);
        }

        [Fact]
        public void LongestCommonSubsequence_FirstLetterSame_FirstLetterUnchanged()
        {
            var segments = EditDistance.LongestCommonSubsequence("ab", "ac");
            segments.Should().HaveCount(3);
            segments[0].Should().Be(new EditSegment(EditSegmentType.Unchanged, "a"));
            segments
                .Where(segment => segment.Type is EditSegmentType.Added)
                .Should()
                .ContainSingle()
                .Which.Content.Should().Be("c");
            segments
                .Where(segment => segment.Type is EditSegmentType.Removed)
                .Should()
                .ContainSingle()
                .Which.Content.Should().Be("b");
        }
        
        [Fact]
        public void LongestCommonSubsequence_LastLetterSame_LastLetterUnchanged()
        {
            var segments = EditDistance.LongestCommonSubsequence("ba", "ca");
            segments.Should().HaveCount(3);
            segments[2].Should().Be(new EditSegment(EditSegmentType.Unchanged, "a"));
            segments
                .Where(segment => segment.Type is EditSegmentType.Added)
                .Should()
                .ContainSingle()
                .Which.Content.Should().Be("c");
            segments
                .Where(segment => segment.Type is EditSegmentType.Removed)
                .Should()
                .ContainSingle()
                .Which.Content.Should().Be("b");
        }
        
        [Theory]
        [InlineData("hello", "world")]
        [InlineData("foo", "bar")]
        [InlineData("cart", "sat")]
        [InlineData("orange", "pineapple")]
        [InlineData("watermelon", "apricot")]
        public void LongestCommonSubsequence_VariousSourceDestination_CanReconstructSourceAndDestination(string source, string destination)
        {
            var segments = EditDistance.LongestCommonSubsequence(source, destination);
            
            var reconstructedSource = string.Join("", segments
                .Where(segment => segment.Type is EditSegmentType.Unchanged or EditSegmentType.Removed)
                .Select(segment => segment.Content));
            var reconstructedDestination = string.Join("", segments
                .Where(segment => segment.Type is EditSegmentType.Unchanged or EditSegmentType.Added)
                .Select(segment => segment.Content));
            
            reconstructedSource.Should().Be(source);
            reconstructedDestination.Should().Be(destination);
        }
        
        
    }
}
