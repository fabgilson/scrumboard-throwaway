using Blazorise;
using Bunit;
using Grpc.Core;
using LensCoreDashboard.Shared;
using LensCoreDashboard.Tests.Helpers;
using Moq;
using SharedLensResources;

namespace LensCoreDashboard.Tests.Blazor.Components;

public class CreateUserComponentTest : BaseContextEnabledTest
{
    [Fact]
    public void ValidUserDataPresent_HitSubmit_SendsCorrectRpcCall()
    {
        var cut = CreateComponent<CreateUser>();
        
        var response = CallHelpers.CreateAsyncUnaryCall(new CreateNewLensAccountResponse
        {
            Validation = new ValidationResponse(),
            UserResponse = new UserResponse()
        });
        LensAuthClientMock.Setup(x => x.CreateNewLensAccountAsync(
            It.IsAny<CreateNewLensAccountRequest>(),
            null, null, CancellationToken.None)
        ).Returns(response);
        
        cut.Find("#username-input").Input("abc123");
        cut.Find("#password-input").Input("P@ssword123!");
        cut.Find("#email-input").Input("abc123@scrumboard.com");
        cut.Find("#first-name-input").Input("Abby");
        cut.Find("#last-name-input").Input("Agile");
        cut.Find("#create-user-submit-button").Click();

        LensAuthClientMock.Verify(x => x.CreateNewLensAccountAsync(new CreateNewLensAccountRequest
        {
            UserName = "abc123",
            Email = "abc123@scrumboard.com",
            FirstName = "Abby",
            LastName = "Agile",
            Password = "P@ssword123!"
        }, It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once());
    }
}