using Bunit;
using FluentAssertions;
using Xunit;
using ScrumBoard.Shared.Widgets;

namespace ScrumBoard.Tests.Blazor
{
    public class PageButtonsComponentTest : TestContext
    {
        private IRenderedComponent<PageButtons> _component;

        private int _totalPages;
        
        private int _currentPage;

        private void ChangePage(int newPage) {
            _currentPage = newPage;
        }

        private void CreateComponent(int currentPages=1, int totalPages=1) {
            _currentPage = currentPages; 
            _totalPages = totalPages;
            _component = RenderComponent<PageButtons>(parameters => parameters
                .Add(c => c.TotalPages, _totalPages)
                .Add(c => c.CurrentPage, _currentPage)
                .Add(c => c.CurrentPageChanged, (newPage) => ChangePage(newPage))
            );
        }
 
        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(5, 5)]
        [InlineData(100, 5)]
        public void ComponentRendered_GivenNumberOfPages_CorrectNumberOfPageButtonsRender(int numberOfPages, int expected) {
            CreateComponent(totalPages: numberOfPages);
            var buttons =  _component.FindAll(".page-button");
            buttons.Should().HaveCount(expected);
        }

        [Fact]
        public void ClickPageTwo_OnPageOneWithTwoPages_PageChangesToPageTwo() {
            CreateComponent(1, 2);
            _component.Find("#page-2-button").Click();
            _currentPage.Should().Be(2);
        }

        [Fact]
        public void ClickNextPage_OnPageOneWithTwoPages_PageChangesToPageTwo() {
            CreateComponent(1, 2);
            _component.Find("#next-page-button").Click();
            _currentPage.Should().Be(2);
        }

        [Fact]
        public void ClickNextPage_OnPageTwoWithTwoPages_PageStaysPageTwo() {
            CreateComponent(2, 2);
            _component.Find("#next-page-button").Click();
            _currentPage.Should().Be(2);
        }

        [Fact]
        public void ClickPreviosPage_OnPageTwoWithTwoPages_PageChangesToPageOne() {
            CreateComponent(2, 2);
            _component.Find("#previous-page-button").Click();
            _currentPage.Should().Be(1);
        }

        [Fact]
        public void ClickPreviousPage_OnPageOneWithTwoPages_PageStaysPageOne() {
            CreateComponent(1, 2);
            _component.Find("#previous-page-button").Click();
            _currentPage.Should().Be(1);
        }
    }
}