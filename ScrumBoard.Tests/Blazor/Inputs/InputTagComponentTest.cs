using Bunit;
using FluentAssertions;
using ScrumBoard.Shared.Inputs;
using System.Collections.Generic;
using Xunit;
using ScrumBoard.Models.Entities;
using Moq;
using System;
using System.Threading.Tasks;
using ScrumBoard.Models;
using System.Linq;

namespace ScrumBoard.Tests.Blazor.Inputs
{
    public class Tag : ITag
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public BadgeStyle Style { get; set; }
    }

    public class InputTagComponentTest : TestContext
    {
        private IRenderedComponent<InputTag<Tag>> _component;
        
        private readonly Mock<Action<ICollection<Tag>>> _onValueChanged = new();

        private const int TagLimit = 5;
        

        private static readonly Tag Chore   = new() { Name = "Chore",   Id = 1 };
        private static readonly Tag Feature = new() { Name = "Feature", Id = 2 };
        private static readonly Tag Fix     = new() { Name = "Fix",     Id = 3 };

        public InputTagComponentTest()
        {
            RemakeComponent(initiallyDisabled: false);
        }

        private void RemakeComponent(bool initiallyDisabled)
        {
            var tagProvider = Task.FromResult(new List<Tag>() { Chore, Feature, Fix });
            _component = RenderComponent<InputTag<Tag>>(
                parameters => parameters
                    .Add(cut => cut.ValueChanged, _onValueChanged.Object)
                    .Add(cut => cut.Value, new List<Tag>())
                    .Add(cut => cut.TagProvider, tagProvider)
                    .Add(cut => cut.Disabled, initiallyDisabled)
            );
        }

        private void SetValue(ICollection<Tag> tags, bool disabled, bool limitShown = false) {
            _component.SetParametersAndRender(parameters => parameters
                .Add(cut => cut.Value, tags)
                .Add(cut => cut.Disabled, disabled)
                .Add(cut => cut.LimitShown, limitShown)
            );
        }

        [Fact]
        public void SelectTag_ClickCorrespondingItemInDropdown_TagSelected()
        {
            _component.Find($"#tag-select-{Chore.Id}").Click();
            _onValueChanged.Verify(mock => mock(new List<Tag>() { Chore }), Times.Once());
        }
        
        [Fact]
        public void SelectTag_TagAlreadySelected_TwoTagsSelected()
        {
            SetValue(new List<Tag>() { Fix }, disabled: false);
            _component.Find($"#tag-select-{Feature.Id}").Click();
            _onValueChanged.Verify(mock => mock(new List<Tag>() { Fix, Feature }), Times.Once());
        }

        [Fact]
        public void SelectTag_ClickDeleteButton_TagRemoved()
        {
            var initial = new List<Tag>() {Feature, Fix, Chore};
            SetValue(initial, disabled: false);
            _component.Find($"#tag-delete-{Fix.Id}").Click();
            _onValueChanged.Verify(mock => mock(new List<Tag>() { Feature, Chore }), Times.Once());
        }
        
        [Fact]
        public void Disabled_DeleteButton_NoneExist()
        {
            var initial = new List<Tag>() {Feature, Fix, Chore};
            SetValue(initial, disabled: true);
            _component.FindAll($"#tag-delete-{Feature.Id}").Should().BeEmpty();
            _component.FindAll($"#tag-delete-{Fix.Id}").Should().BeEmpty();
            _component.FindAll($"#tag-delete-{Chore.Id}").Should().BeEmpty();
        }
        
        [Fact]
        public void Disabled_SelectButton_NoneExist()
        {
            SetValue(new List<Tag>(), disabled: true);
            _component.FindAll($"#tag-select-{Feature.Id}").Should().BeEmpty();
            _component.FindAll($"#tag-select-{Fix.Id}").Should().BeEmpty();
            _component.FindAll($"#tag-select-{Chore.Id}").Should().BeEmpty();
        }

        [Fact]
        public void NoTags_PlaceholderShown()
        {
            SetValue(new List<Tag>(), disabled: false);
            _component.FindAll("#no-tags-placeholder").Should().ContainSingle();
        
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ManyTags_WithoutLimit_AllShown(bool initiallyDisabled) 
        {
            var repeats = 20;
            RemakeComponent(initiallyDisabled);
            SetValue(Enumerable.Repeat(Chore, repeats).ToList(), disabled: false, limitShown: false);
            _component.FindAll($"div.badge:contains(\"{Chore.Name}\")").Should().HaveCount(repeats);
            _component.FindAll("#toggle-show-tags").Should().BeEmpty();
        }
        
        [Fact]
        public void ManyTags_InitiallyEnabledWithLimit_AllShown() 
        {
            var repeats = 20;
            RemakeComponent(false);
            SetValue(Enumerable.Repeat(Chore, repeats).ToList(), disabled: false, limitShown: true);
            _component.FindAll($"div.badge:contains(\"{Chore.Name}\")").Should().HaveCount(repeats);
        }
        
        [Fact]
        public void ManyTags_InitiallyEnabledWithLimitAndClickShowFewer_SomeShown() 
        {
            RemakeComponent(false);
            SetValue(Enumerable.Repeat(Chore, 20).ToList(), disabled: false, limitShown: true);
            _component.Find("#toggle-show-tags").Click();
            _component.FindAll($"div.badge:contains(\"{Chore.Name}\")").Should().HaveCount(TagLimit - 1);
        }

        [Fact]
        public void ManyTags_InitiallyDisabledWithLimit_SomeShown()
        {
            RemakeComponent(true);
            SetValue(Enumerable.Repeat(Chore, 20).ToList(), disabled: false, limitShown: true);
            _component.FindAll($"div.badge:contains(\"{Chore.Name}\")").Should().HaveCount(TagLimit - 1);
            _component.FindAll("#toggle-show-tags").Should().ContainSingle();
        }
        
        [Fact]
        public void TagsAtLimit_InitiallyDisabledWithLimit_AllShown()
        {
            RemakeComponent(true);
            SetValue(Enumerable.Repeat(Chore, TagLimit).ToList(), disabled: false, limitShown: true);
            _component.FindAll($"div.badge:contains(\"{Chore.Name}\")").Should().HaveCount(TagLimit);
            _component.FindAll("#toggle-show-tags").Should().BeEmpty();
        }

        [Fact]
        public void ManyTags_InitiallyDisabledWithLimitAndClickShowMore_AllShown()
        {
            RemakeComponent(true);
            var repeats = 20;
            SetValue(Enumerable.Repeat(Chore, repeats).ToList(), disabled: false, limitShown: true);
            _component.Find("#toggle-show-tags").Click();
            _component.FindAll($"div.badge:contains(\"{Chore.Name}\")").Should().HaveCount(repeats);
        }
    }
}
