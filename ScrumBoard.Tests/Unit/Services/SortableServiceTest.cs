using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.Models;
using ScrumBoard.Services;
using Xunit;

namespace ScrumBoard.Tests.Unit.Services
{
    public class SortableServiceTest 
    {
        private ISortableService<int> _sortableService;

        public SortableServiceTest() {
            var logger = new Mock<ILogger<SortableService<int>>>();
            _sortableService = new SortableService<int>(logger.Object);
        }

        [Fact]
        public void Register_Called_IncrementsAndReturnsKey() {
            _sortableService.Register(new Mock<ISortableList<int>>().Object).Should().Be(1);
            _sortableService.Register(new Mock<ISortableList<int>>().Object).Should().Be(2);
            _sortableService.Register(new Mock<ISortableList<int>>().Object).Should().Be(3);
        }

        [Fact]
        public async Task HandleEvent_WithinSingleList_NotifiesListOfUpdate() {
            var mockList = new Mock<ISortableList<int>>();
            mockList.SetupGet(mock => mock.Items).Returns(new List<int>() { 1, 2, 3, 4});

            var key = _sortableService.Register(mockList.Object);

            await _sortableService.HandleEvent(new SortableEventArgs() { From = key, To = key, OldIndex = 0, NewIndex = 3});

            mockList.Verify(mock => mock.TriggerItemsChanged(new List<int>() { 2, 3, 4, 1 }), Times.Once());
        }

        [Fact]
        public async Task HandleEvent_AccrossLists_NotifiesBothListsOfUpdate() {
            var mockStartList = new Mock<ISortableList<int>>();
            mockStartList.SetupGet(mock => mock.Items).Returns(new List<int>() { 1, 2});
            var startKey = _sortableService.Register(mockStartList.Object);


            var mockEndList = new Mock<ISortableList<int>>();
            mockEndList.SetupGet(mock => mock.Items).Returns(new List<int>() { 3, 4});
            var endKey = _sortableService.Register(mockEndList.Object);

            await _sortableService.HandleEvent(new SortableEventArgs() { From = startKey, To = endKey, OldIndex = 0, NewIndex = 2});

            mockStartList.Verify(mock => mock.TriggerItemsChanged(new List<int>() { 2 }), Times.Once());
            mockEndList.Verify(mock => mock.TriggerItemsChanged(new List<int>() { 3, 4, 1}), Times.Once());

            mockEndList.Verify(mock => mock.TriggerItemAdded(1), Times.Once());
        }

        [Fact]
        public void SynchronizeGroup_MembersInGroup_MembersInformed() {
            var list1 = new Mock<ISortableList<int>>();
            list1.SetupGet(mock => mock.Group).Returns("test_group");
            var list2 = new Mock<ISortableList<int>>();
            list2.SetupGet(mock => mock.Group).Returns("test_group");

            var listBystander = new Mock<ISortableList<int>>();
            listBystander.SetupGet(mock => mock.Group).Returns("other_group");

            _sortableService.Register(list1.Object);
            _sortableService.Register(list2.Object);
            _sortableService.Register(listBystander.Object);

            _sortableService.SynchronizeGroup("test_group");

            list1.Verify(mock => mock.Synchronize(), Times.Once());
            list2.Verify(mock => mock.Synchronize(), Times.Once());
            listBystander.Verify(mock => mock.Synchronize(), Times.Never());
        }

        [Fact]
        public void Unregister_WithOtherMembersInGroup_RemainingGroupMembersSynchronized() {
            var list1 = new Mock<ISortableList<int>>();
            list1.SetupGet(mock => mock.Group).Returns("test_group");
            var list2 = new Mock<ISortableList<int>>();
            list2.SetupGet(mock => mock.Group).Returns("test_group");

            var key1 = _sortableService.Register(list1.Object);
            _sortableService.Register(list2.Object);

            _sortableService.Unregister(key1);

            list1.Verify(mock => mock.Synchronize(), Times.Never());
            list2.Verify(mock => mock.Synchronize(), Times.Once());
        }
    }
}