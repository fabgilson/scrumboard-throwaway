using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharpWrappers;
using Bunit;
using FluentAssertions;
using FluentAssertions.Execution;
using LensCoreDashboard.Shared;
using LensCoreDashboard.Tests.Helpers;
using Moq;
using SharedLensResources;

namespace LensCoreDashboard.Tests.Blazor.Components;

public class UserTableComponentTest : BaseContextEnabledTest
{
    private IRenderedComponent<UserTable> CreateComponentUnderTest(IReadOnlyCollection<UserResponse> userResponses)
    {
        var response = CallHelpers.CreateAsyncUnaryCall(new PaginatedUsersResponse
        {
            UserResponses = { userResponses },
            PaginationResponseOptions = new PaginationResponseOptions { ResultSetSize = userResponses.Count }
        });

        LensUserClientMock.Setup(x => x.GetPaginatedUsersAsync(It.IsAny<GetPaginatedUsersRequest>(), null, null, CancellationToken.None))
            .Returns(response);

        LensUserClientMock.Setup(x => x.UpdateUserAsync(It.IsAny<UpdateUserRequest>(), null, null, CancellationToken.None))
            .Returns(CallHelpers.CreateAsyncUnaryCall(new ValidationResponse()));
        
        return CreateComponent<UserTable>();
    }
    
    [Fact]
    public void Rendered_MultipleUserResults_UsersCorrectlyDisplayed()
    {
        var userResponses = new [] {
            SampleDataHelper.CreateNewUserResponse(),
            SampleDataHelper.CreateNewUserResponse(),
            SampleDataHelper.CreateNewUserResponse(),
        };

        var cut = CreateComponentUnderTest(userResponses);
        var tableBody = cut.Find("#user-table-body");
        
        tableBody.ChildElementCount.Should().Be(3);
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UserShownInTable_EditButtonClicked_FieldsBecomeEditable(bool isLensUser)
    {
        var userToEdit = SampleDataHelper.CreateNewUserResponse(identitySource: isLensUser ? "Lens" : "UcLdap");
        var cut = CreateComponentUnderTest(new[] { userToEdit });
        cut.Find("#start-editing-button").Click();

        using (new AssertionScope())
        {
            // Lens Users may have their username modified, LDAP users may not
            if (isLensUser)
            {
                cut.Find("#username-input").Unwrap()
                    .Should().Match<IHtmlInputElement>(x => x.Value == userToEdit.UserName);
            }
            else
            {
                cut.FindAll("#username-input").Should().BeEmpty();
            }
            cut.Find("#first-name-input").Unwrap()
                .Should().Match<IHtmlInputElement>(x => x.Value == userToEdit.FirstName);
            cut.Find("#last-name-input").Unwrap()
                .Should().Match<IHtmlInputElement>(x => x.Value == userToEdit.LastName);
            cut.Find("#email-input").Unwrap()
                .Should().Match<IHtmlInputElement>(x => x.Value == userToEdit.Email);
            cut.Find("#role-input").Unwrap()
                .Should().Match<IHtmlSelectElement>(x => x.SelectedOptions.Length == 1 && x.SelectedOptions.First().Value == userToEdit.LensRole.ToString());
        }
    }
    
    [Fact]
    public void EditingAUser_AllFieldsChangedAndSubmitted_CorrectRpcCallSent()
    {
        var userToEdit = SampleDataHelper.CreateNewUserResponse();
        var cut = CreateComponentUnderTest(new[] { userToEdit });
        cut.Find("#start-editing-button").Click();

        var expectedUpdateRequest = new UpdateUserRequest
        {
            UserId = userToEdit.Id,
            NewEmail = "new.email@gmail.com",
            NewFirstName = "Bobby",
            NewLastName = "Brown",
            NewUserName = "bobby.brown",
            NewRole = GlobalLensRole.SystemAdmin
        };
        
        cut.Find("#username-input").Input(expectedUpdateRequest.NewUserName);
        cut.Find("#first-name-input").Input(expectedUpdateRequest.NewFirstName);
        cut.Find("#last-name-input").Input(expectedUpdateRequest.NewLastName);
        cut.Find("#email-input").Input(expectedUpdateRequest.NewEmail);

        var roleSelector = (cut.Find("#role-input").Unwrap() as IHtmlSelectElement)!;
        roleSelector.Value = roleSelector.Options[1].Value;
        roleSelector.Change(roleSelector.Value);
        
        cut.Find("#save-editing-user-button").Click();

        using (new AssertionScope())
        {
            LensUserClientMock.Verify(x => x.UpdateUserAsync(expectedUpdateRequest, null, null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}