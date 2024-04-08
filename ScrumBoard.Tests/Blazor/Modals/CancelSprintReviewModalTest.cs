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
    public class CancelSprintReviewModalTest : TestContext
    {
        private readonly IRenderedComponent<CancelSprintReviewModal> _component;

        private Sprint _sprint;

        public CancelSprintReviewModalTest() 
        {
            _sprint = new() {
                Name = "Test sprint",
            };

            // Add dummy ModalTrigger
            ComponentFactories.Add(new ModalTriggerComponentFactory());
            _component = RenderComponent<CancelSprintReviewModal>();
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
        public async Task Showing_ConfirmPressed_FalseReturned()
        {
            var resultTask = await Show();
            _component.Find("#cancel-review-sprint").Click();
            (await resultTask).Should().BeFalse();
        }
    }
}
