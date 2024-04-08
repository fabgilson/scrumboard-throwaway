using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Tests.Util;
using SharedLensResources;
using SharedLensResources.Authentication;
using Xunit;

namespace ScrumBoard.Tests.Blazor
{
    public class LoginComponentTest : TestContext
    {
        // Register mocks as class-level variables so methods can assert whether mock methods were called
        private Mock<IAuthenticationService> mockAuthService;
        private Mock<IUserRepository> mockUserRepository;
        private Mock<IScrumBoardStateStorageService> mockStateStorageService;
        private Mock<ISeedDataService> mockSeedDataService;
        private Mock<ISprintRepository> mockSprintRepository;
        private Mock<IConfigurationService> mockConfigurationService;
        private Mock<IUsageDataService> mockUsageDataService;

        /// <summary>
        /// Creates a rendered login component, complete with an already mocked
        /// </summary>
        /// <returns>Rendered login component ready for testing</returns>
        private IRenderedComponent<Login> CreateLoginComponent(LensAuthenticationReply mockedLoginResponse)
        {
            // Mock authentication service and inject this mock so that the login attempt returns whatever `mockedLoginResponse` is
            mockAuthService = new Mock<IAuthenticationService>();
            mockAuthService
                .Setup(x => x.AttemptLogin(It.IsAny<LensAuthenticateRequest>()))
                .Returns<LensAuthenticateRequest>((_) => Task.FromResult(mockedLoginResponse));
            Services.AddScoped(_ => mockAuthService.Object);

            mockUserRepository = new Mock<IUserRepository>();
            Services.AddScoped(_ => mockUserRepository.Object);

            mockStateStorageService = new Mock<IScrumBoardStateStorageService>();
            Services.AddScoped(_ => mockStateStorageService.Object);

            mockSeedDataService = new();
            Services.AddScoped(_ => mockSeedDataService.Object);

            mockSprintRepository = new();
            Services.AddScoped(_ => mockSprintRepository.Object);

            mockConfigurationService = new();
            Services.AddScoped(_ => mockConfigurationService.Object);

            mockUsageDataService = new();
            Services.AddTransient(_ => mockUsageDataService.Object);

            // Render the login component and fill with some test data
            return RenderComponent<Login>();
        }

        /// <summary>
        /// When all is well before submitting, no error messages should be visible. So either they should be hidden, or non-existant
        /// </summary>
        /// <param name="errorMessageCssSelector">CSS selector for error lables that shouldn't be visible</param>
        [Theory]
        [InlineData("#error-label")]
        [InlineData("#username-validation-message")]
        [InlineData("#password-validation-message")]
        public void PageLoaded_NoUserInput_ErrorMessagesHidden(string errorMessageCssSelector)
        {
            var loginComponentUnderTest = CreateLoginComponent(null);

            // Check that error label either doesn't exist, or exists but is hidden
            var matchingErrorLabels = loginComponentUnderTest.FindAll(errorMessageCssSelector);
            if (!matchingErrorLabels.Any()) return;
            matchingErrorLabels.Should().ContainSingle();
            var errorLabel = matchingErrorLabels[0];
            errorLabel.GetAttribute("hidden").Should().BeOneOf("", "true"); // `hidden=true` or simply `hidden` both work to hide element
        }

        /// <summary>
        /// When all is well after submitting, no error messages should be visible. So either they should be hidden, or non-existant
        /// </summary>
        /// <param name="errorMessageCssSelector">CSS selector for error lables that shouldn't be visible</param>
        [Theory]
        [InlineData("#error-label")]
        [InlineData("#username-validation-message")]
        [InlineData("#password-validation-message")]
        public void LoginAttempt_Successful_ErrorMessagesHidden(string errorMessageCssSelector)
        {
            var loginComponentUnderTest = CreateLoginComponent(new LensAuthenticationReply { Success = true, UserResponse = new UserResponse{ Id = 100 }});

            var usernameInput = loginComponentUnderTest.Find("#username-input");
            var passwordInput = loginComponentUnderTest.Find("#password-input");
            usernameInput.Change("testUser");
            passwordInput.Change("Password123!");

            loginComponentUnderTest.Find("#login-form").Submit();
            loginComponentUnderTest.WaitForState(() => loginComponentUnderTest.GetChangesSinceFirstRender().Any());

            // Check that error label either doesn't exist, or exists but is hidden
            var matchingErrorLabels = loginComponentUnderTest.FindAll(errorMessageCssSelector);
            if (matchingErrorLabels.Any())
            {
                matchingErrorLabels.Should().ContainSingle();
                var errorLabel = matchingErrorLabels.First();
                errorLabel.GetAttribute("hidden").Should().BeOneOf("", "true"); // `hidden=true` or simply `hidden` both work to hide element
            }
        }

        [Fact]
        public void LoginAttempt_ErrorReturned_ErrorMessageDisplayedCorrectly()
        {
            var authResponseMessage = "Failed to login, please try again later!";
            var loginComponentUnderTest = CreateLoginComponent(new LensAuthenticationReply { Success = false, Message = authResponseMessage });

            var usernameInput = loginComponentUnderTest.Find("#username-input");
            var passwordInput = loginComponentUnderTest.Find("#password-input");
            usernameInput.Change("testUser");
            passwordInput.Change("Password123!");

            // Attempt to login, and wait until a change is spotted or a timeout occurs (login is an async operation)
            var errorLabel = loginComponentUnderTest.Find("#error-label");
            var startingErrorText = errorLabel.TextContent;
            loginComponentUnderTest.Find("#login-form").Submit();
            loginComponentUnderTest.WaitForState(() => errorLabel.TextContent != startingErrorText);

            // Verify that the system handles failure as expected
            errorLabel.TextContent.Should().EndWith(authResponseMessage);
            errorLabel.GetAttribute("hidden").Should().BeOneOf("false", null);
        }

        [Fact]
        public void LoginAttempt_EmptyUsername_ErrorMessageDisplayedCorrectly()
        {
            var loginComponentUnderTest = CreateLoginComponent(null);

            var passwordInput = loginComponentUnderTest.Find("#password-input");
            passwordInput.Change("Password123!");

            // Attempt to login, and wait until a change is spotted or a timeout occurs (login is an async operation)
            loginComponentUnderTest.Find("#login-form").Submit();
            loginComponentUnderTest.WaitForState(() => loginComponentUnderTest.FindAll("#username-validation-message").Any());
            var errorLabel = loginComponentUnderTest.Find("#username-validation-message");

            // Verify that the system handles failure as expected
            // Here we use reflection to get the error message for failing the RequiredAttribute of username, to check it is shown
            var expectedErrorMessage = typeof(LoginForm).GetAttribute<RequiredAttribute>("Username").ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
            errorLabel.GetAttribute("hidden").Should().BeOneOf("false", null);
        }

        [Fact]
        public void LoginAttempt_EmptyPassword_ErrorMessageDisplayedCorrectly()
        {
            var loginComponentUnderTest = CreateLoginComponent(null);

            var usernameInput = loginComponentUnderTest.Find("#username-input");
            usernameInput.Change("testUser");

            // Attempt to login, and wait until a change is spotted or a timeout occurs (login is an async operation)
            loginComponentUnderTest.Find("#login-form").Submit();
            loginComponentUnderTest.WaitForState(() => loginComponentUnderTest.FindAll("#password-validation-message").Any());
            var errorLabel = loginComponentUnderTest.Find("#password-validation-message");

            // Verify that the system handles failure as expected
            var expectedErrorMessage = typeof(LoginForm).GetAttribute<RequiredAttribute>("Password").ErrorMessage;
            errorLabel.TextContent.Should().Be(expectedErrorMessage);
            errorLabel.GetAttribute("hidden").Should().BeOneOf("false", null);
        }

        [Fact]
        public void LoginAttempt_ValidInputs_CallsAuthenticateWithGivenCredentials()
        {
            var loginComponentUnderTest = CreateLoginComponent(new LensAuthenticationReply());
            var username = "abc123";
            var password = "Pass123!@#";

            // Enter valid data so that validator won't complain, and then submit the form
            loginComponentUnderTest.Find("#username-input").Change(username);
            loginComponentUnderTest.Find("#password-input").Change(password);
            loginComponentUnderTest.Find("#login-form").Submit();

            // Verify that AttemptLogin was called exactly once with the given username and password 
            mockAuthService.Verify(mock => mock.AttemptLogin(
                It.Is<LensAuthenticateRequest>(r => r.Username == username && r.Password == password)),
                Times.Once()
            );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LoginAttempt_SuccessfulFirstLogin_PersistsUserLocally(bool seedDataEnabled)
        {
            var loginComponentUnderTest = CreateLoginComponent(new LensAuthenticationReply { Success = true , UserResponse = new UserResponse{ Id = 100 }});
            mockConfigurationService.Setup(mock => mock.SeedDataEnabled).Returns(seedDataEnabled);

            // Enter valid data so that validator won't complain, and then submit the form
            loginComponentUnderTest.Find("#username-input").Change("testUser");
            loginComponentUnderTest.Find("#password-input").Change("Password123!");
            loginComponentUnderTest.Find("#login-form").Submit();

            var capture = new ArgumentCaptor<User>();
            mockUserRepository.Verify(mock => mock.AddAsync(capture.Capture()), Times.Once());
            if (seedDataEnabled) {
                mockSeedDataService.Verify(mock => mock.AddUserToGeneratedProjects(capture.Value));
            }            
        }


        [Fact]
        public void LoginAttempt_SuccessfulReturnVisit_PersistsUserLocally()
        {
            const long testUserId = 7;
            var loginComponentUnderTest = CreateLoginComponent(new LensAuthenticationReply { 
                Success = true, 
                UserResponse = new UserResponse { Id = testUserId }
            });
            mockUserRepository.Setup(mock => mock.GetByIdAsync(testUserId)).ReturnsAsync(new User());

            // Enter valid data so that validator won't complain, and then submit the form
            loginComponentUnderTest.Find("#username-input").Change("testUser");
            loginComponentUnderTest.Find("#password-input").Change("Password123!");
            loginComponentUnderTest.Find("#login-form").Submit();

            mockUserRepository.Verify(mock => mock.AddAsync(It.IsAny<User>()), Times.Never());
        }
    }
}
