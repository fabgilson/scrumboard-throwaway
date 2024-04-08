using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Shared.Inputs;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Inputs
{
    public class InputMemberComponentTest : TestContext
    {
        private IRenderedComponent<InputMember> _component;

        private readonly User _userOne = new() { Id = 101, FirstName = "John", LastName = "Doe" };
        private readonly User _userTwo = new() { Id = 102, FirstName = "Jim", LastName = "Jones" };
        private readonly User _userThree = new() { Id = 103, FirstName = "Jimmy", LastName = "Johnny" };
        private readonly User _self = new() {Id = 42, FirstName = "Jimmy", LastName = "Neutron"};

        private readonly ICollection<User> _allUsers;

        private readonly string _idPrefix = "assignee";

        private readonly Mock<Action<ICollection<User>>> _onValueChanged = new();
        
        public InputMemberComponentTest() {
            _allUsers = new List<User>() { _userOne, _userTwo, _userThree };
        }

        private void CreateComponent(ICollection<User> allUsers, ICollection<User> selectedUsers, int maxUsers = 2, Func<Task<ICollection<User>>> userProvider = null)
        {
            _component = RenderComponent<InputMember>(parameters => parameters
                .Add(p => p.AllUsers, allUsers)
                .Add(p => p.Value, selectedUsers)
                .Add(p => p.MaxUsers, maxUsers)
                .Add(p => p.IdPrefix, _idPrefix)
                .Add(p => p.ValueChanged, _onValueChanged.Object)
                .Add(p => p.UserProvider, userProvider)
                .AddCascadingValue("Self",_self)
            );
        }

        [Fact]
        public void ComponentRenders_WithAllUsers_ContainsAllUsers()
        {
            CreateComponent(_allUsers, _allUsers);
            _component.FindAll(".avatar").Count.Should().Be(3);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        public void ComponentRenders_WithGivenNumberOfUsers_AddUserButtonIsGivenStatus
        (int numberOfUsers, bool addButtonVisible)
        {
            var selectedUsers = _allUsers.Take(numberOfUsers).ToList();

            CreateComponent(_allUsers, selectedUsers);
            var button = _component.FindAll(".add-user");

            if (addButtonVisible) {
                button.Should().ContainSingle();
            } else {
                button.Should().BeEmpty();
            }
        }

        [Fact]
        public void ComponentRenders_AllUsersSelected_DropdownContainsEmptyMessage()
        {
            var users = _allUsers.Take(1).ToList();

            CreateComponent(users, users);
            var button = _component.FindAll(".add-user");
            button.Should().ContainSingle();

            var dropdownItems = _component.FindAll(".dropdown-item");
            dropdownItems.Should().ContainSingle();
            dropdownItems.First().TextContent.Should().Contain("No users available");
        }

        [Theory]
        [InlineData(101)]
        [InlineData(102)]
        [InlineData(103)]
        public void HoverOverUser_UserIsGiven_DisplaysGivenUserName(long userId)
        {
            CreateComponent(_allUsers, _allUsers);
            var user = _component.Instance.Value.First(user => user.Id == userId);
            var userElement = _component.Find($"#user-{userId}");
            userElement.TextContent.Should().Contain(user.FirstName + " " + user.LastName);
        }

        [Fact]
        public void ComponentRenders_UserSelectedFromList_UserAdded()
        {
            CreateComponent(_allUsers, new List<User>());
            
            _component.Find($"#assignee-user-select-{_userTwo.Id}").Click();

            var captor = new ArgumentCaptor<List<User>>();
            _onValueChanged.Verify(mock => mock(captor.Capture()), Times.Once());
            var updatedUsers = captor.Value;

            updatedUsers.Should().BeEquivalentTo(new List<User>() { _userTwo });
        }

        [Fact]
        public void ComponentRenders_UserRemoveButtonPressed_UserRemoved()
        {
            CreateComponent(_allUsers, new List<User>() { _userTwo });
            
            _component.Find(".remove-button").Click();

            var captor = new ArgumentCaptor<List<User>>();
            _onValueChanged.Verify(mock => mock(captor.Capture()), Times.Once());
            var updatedUsers = captor.Value;

            updatedUsers.Should().BeEmpty();
        }

        [Fact]
        public void ComponentRenders_DropdownOpenedWithUserProvider_UserProvidedCalled()
        {
            var mockUserProvider = new Mock<Func<Task<ICollection<User>>>>();
            mockUserProvider.Setup(mock => mock())
                .ReturnsAsync(new List<User>
                {
                    _userOne,
                    _userTwo,
                });
            
            CreateComponent(new List<User>(), new List<User>(), userProvider: mockUserProvider.Object);
                
            mockUserProvider.Verify(mock => mock(), Times.Never());
            
            _component.Find(".add-user").Click();
            
            var dropdownItems = _component.FindAll(".dropdown-item");
            dropdownItems.Should().HaveCount(2);
            
            mockUserProvider.Verify(mock => mock(), Times.Once());
        }
    }
}