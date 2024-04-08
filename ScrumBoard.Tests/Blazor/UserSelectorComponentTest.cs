using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Tests.Blazor.Modals;
using ScrumBoard.Tests.Util;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class UserSelectorComponentTest : TestContext
    {
        private bool _eventCalled;
        private IRenderedComponent<SelectUsers> _componentUnderTest;

        private void CreateSelectUsersComponent(List<User> initialUsers, bool hasRoleChanger = false)
        {
            List<User> selectedUsers = new();    
            _eventCalled = false;
            // Add dummy ModalTrigger
            ComponentFactories.Add(new ModalTriggerComponentFactory());
   
            _componentUnderTest = RenderComponent<SelectUsers>(parameters => parameters
                .Add(p => p.Users, initialUsers)
                .Add(p => p.SelectedUsers, selectedUsers)
                .Add(p => p.RoleChanged, () => { _eventCalled = true; })
                .Add(p => p.HasRoleChanger, hasRoleChanger)
                .AddCascadingValue(FakeDataGenerator.CreateFakeProject())
                .AddCascadingValue("Self", FakeDataGenerator.CreateFakeUser())
            );
        }

        private static List<User> MakeUsers(int count, bool withIds) {
            List<User> userList = new();      

            for (var i = 0; i < count; i++) {
                if (withIds) {
                    userList.Add(new User { Id = i+1, ProjectAssociations = new List<ProjectUserMembership>()});
                } else {
                    userList.Add(new User {ProjectAssociations = new List<ProjectUserMembership>()});
                }               
            }
            return userList;
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(3, 3)]
        [InlineData(50, 50)]
        public void UsersSet_FindListItems_IsEqualToUsersSet(int numberOfUsers, int expectedListItems)
        {
            var testUsers = MakeUsers(numberOfUsers, false);

            CreateSelectUsersComponent(testUsers);

            var listItem = _componentUnderTest.FindComponents<UserListItem>();
            listItem.Count.Should().Be(expectedListItems);
        }

        [Fact]
        public void UsersSet_FindSelectButtonForUser_SelectButtonsAreVisible()
        {
            var testUsers = MakeUsers(3, true);
            CreateSelectUsersComponent(testUsers);

            var selectButtons = _componentUnderTest.FindAll(".select-button");
            selectButtons.Count.Should().Be(testUsers.Count);
        }

        [Fact]
        public void SelectUser_IsNotCalled_RemoveButtonNotVisible()
        {
            var testUsers = MakeUsers(3, true);
            CreateSelectUsersComponent(testUsers);

            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll(".remove-button").Count == 0);
        }

        [Fact]
        public void SelectUser_ButtonIsClicked_RemoveButtonIsVisible()
        {
            var testUsers = MakeUsers(3, true);
            CreateSelectUsersComponent(testUsers);

            _componentUnderTest.Find("#select-user-1").Click();

            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll(".remove-button").Count == 1);
        }

        [Fact]
        public void SelectUser_ButtonIsClicked_SelectButtonNoLongerVisible()
        {
            var testUsers = MakeUsers(3, true);
            CreateSelectUsersComponent(testUsers);

            _componentUnderTest.Find("#select-user-1").Click();

            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll(".select-button").Count == testUsers.Count-1);
        }

        [Fact]
        public void SelectedUsersSet_FindRemoveButtons_RemoveButtonIsVisible()
        { 
            var testUsers = MakeUsers(1, true);
            CreateSelectUsersComponent(testUsers);
            _componentUnderTest.Find("#select-user-1").Click();
        
            _componentUnderTest.FindAll(".remove-button").Count.Should().Be(1);
        }

        [Fact]
        public void ChangeRole_NewRoleClicked_EventCallbackTriggered()
        {
            var testUsers = MakeUsers(1, true);
            CreateSelectUsersComponent(testUsers, true);
            _componentUnderTest.Find("#select-user-1").Click();
            
            _componentUnderTest.Find("#role-changer-select-1").Change("Reviewer");

            _eventCalled.Should().BeTrue();
        }

        // Confirmation modal related tests

        [Fact]
        public void RemoveButton_RemoveButtonClicked_ConfirmationModalVisible()
        {
            var testUsers = MakeUsers(1, true);
            CreateSelectUsersComponent(testUsers);
            _componentUnderTest.Find("#select-user-1").Click();
        
            _componentUnderTest.Find("#remove-user-1").Click();

            _componentUnderTest.FindAll(".modal.show").Should().ContainSingle();            
        }

        [Fact]
        public void RemoveButton_ConfirmRemoveButtonClicked_SelectButtonIsVisible()
        {
            var testUsers = MakeUsers(1, true);
            CreateSelectUsersComponent(testUsers);
            _componentUnderTest.Find("#select-user-1").Click();
        
            _componentUnderTest.Find("#remove-user-1").Click();

            _componentUnderTest.Find("#confirm-user-removal").Click();

            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll(".select-button").Count == 1);
        }

        [Fact]
        public void RemoveButton_ConfirmRemoveButtonClicked_RemoveButtonNoLongerVisible()
        {
            var testUsers = MakeUsers(1, true);
            CreateSelectUsersComponent(testUsers);
            _componentUnderTest.Find("#select-user-1").Click();
        
            _componentUnderTest.Find("#remove-user-1").Click();

            _componentUnderTest.Find("#confirm-user-removal").Click();

            _componentUnderTest.WaitForState(() => _componentUnderTest.FindAll(".remove-button").Count == 0);
        }

        [Fact]
        public void ModalCloseButton_CloseButtonClicked_ConfirmationModalNotVisible()
        {
            var testUsers = MakeUsers(1, true);
            CreateSelectUsersComponent(testUsers);
            _componentUnderTest.Find("#select-user-1").Click();
        
            _componentUnderTest.Find("#remove-user-1").Click();

            _componentUnderTest.Find("#close-modal").Click();

            _componentUnderTest.FindAll(".modal.show").Should().BeEmpty();            
        }
    }
}