using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using FluentAssertions;
using Xunit;
using ScrumBoard.Shared.Modals;
using System.Threading.Tasks;
using ScrumBoard.Tests.Util;
using Moq;
using ScrumBoard.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using ScrumBoard.Repositories;

namespace ScrumBoard.Tests.Blazor.Modals
{
    public class SkipSprintReviewModalTest : TestContext
    {
        private readonly IRenderedComponent<SkipSprintReviewModal> _component;

        private Sprint _sprint;

        public SkipSprintReviewModalTest() 
        {
            _sprint = new() {
                Name = "Test sprint",
            };

            // Add dummy ModalTrigger
            ComponentFactories.Add(new ModalTriggerComponentFactory());
            _component = RenderComponent<SkipSprintReviewModal>();
        }

        /// <summary>
        /// Shows the modal with _sprint as sprint to cancel the review of
        /// </summary>
        /// <returns>
        /// Task that will complete when the modal is shown, which contains another task for when the modal returns a value
        /// </returns>
        private async Task<Task<bool>> Show()
        {
            Task<bool> showResultTask = null;
            await _component.InvokeAsync(() =>
            {
                showResultTask = _component.Instance.Show(_sprint);
            });
            return showResultTask;
        }
        
        [Fact]
        public async Task Show_Called_ModalShown()
        {
            _component.FindAll(".modal-body").Should().BeEmpty();
            await Show();
            _component.FindAll(".modal-body").Should().NotBeEmpty();
        }

        [Theory]
        [InlineData(".btn-close")]
        [InlineData("#close-modal")]
        public async Task Showing_ACloseButtonPressed_TrueReturned(string closeButtonSelector)
        {
            var resultTask = await Show();
            _component.Find(closeButtonSelector).Click();
            (await resultTask).Should().BeTrue();
        }

        [Fact]
        public async Task Showing_NotEnteredConfirmText_ConfirmDisabled()
        {
            await Show();
            _component.Find("#confirm-skip-sprint-review").ClassList.Should().Contain("disabled");
        }
        
        [Fact]
        public async Task Showing_EnteredWrongConfirmText_ConfirmDisabled()
        {
            await Show();
            _component.Find("#confirm-input").Input("don't skip");
            
            _component.Find("#confirm-skip-sprint-review").ClassList.Should().Contain("disabled");
        }

        [Fact]
        public async Task Showing_ConfirmWithConfirmText_FalseReturned()
        {
            var resultTask = await Show();
            
            _component.Find("#confirm-input").Input(" skip Review");

            var confirmButton = _component.Find("#confirm-skip-sprint-review");
            confirmButton.ClassList.Should().NotContain("disabled");
            confirmButton.Click();

            (await resultTask).Should().BeFalse();
        }
    }
}
