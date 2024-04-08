using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.Services.StateStorage;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SharedLensResources.Blazor.StateManagement;
using Xunit;

namespace ScrumBoard.Tests.Unit.Services
{
    public class ScrumBoardStateStorageServiceTest
    {
        private readonly Mock<IProtectedLocalStorageWrapper> _localStorageServiceMock;
        private readonly Mock<IProtectedSessionStorageWrapper> _sessionStorageServiceMock;
        private readonly ScrumBoardStateStorageService _service;
        private readonly Mock<ILogger<ScrumBoardStateStorageService>> _loggerMock;

        public ScrumBoardStateStorageServiceTest() {
            _localStorageServiceMock = new Mock<IProtectedLocalStorageWrapper>();
            _sessionStorageServiceMock = new Mock<IProtectedSessionStorageWrapper>();
            _loggerMock = new Mock<ILogger<ScrumBoardStateStorageService>>();
            _service = new ScrumBoardStateStorageService(_localStorageServiceMock.Object, _sessionStorageServiceMock.Object, _loggerMock.Object);
        }
        
        private void VerifyLoggerWasCalled()
        {
            // Logger.logWarning etc are static methods, we have to check the instance methods that they call
            _loggerMock.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));
        }

        [Fact]
        public async Task SetBearerTokenAsync_LoginNotRemembered_AddsToSessionStorage() {
            var token = "Foo";
            await _service.SetBearerTokenAsync(token, false);
            _sessionStorageServiceMock.Verify(session => session.SetAsync("ScrumBoardBearerTokenPurpose", "BEARER_TOKEN", token));
            _localStorageServiceMock.Verify(local => local.SetAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SetBearerTokenAsync_LoginRemembered_AddsToLocalStorage()
        {
            var token = "Foo";
            await _service.SetBearerTokenAsync(token, true);
            _localStorageServiceMock.Verify(mock => mock.SetAsync("ScrumBoardBearerTokenPurpose", "BEARER_TOKEN", token));
            _sessionStorageServiceMock.Verify(local => local.SetAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetBearerTokenAsync_ValidToken_DeletesAnyExistingTokens(bool isLoginRemembered)
        {
            var token = "Foo";
            await _service.SetBearerTokenAsync(token, isLoginRemembered);

            _localStorageServiceMock.Verify(mock => mock.DeleteAsync("BEARER_TOKEN"));
            _sessionStorageServiceMock.Verify(mock => mock.DeleteAsync("BEARER_TOKEN"));
        }

        [Fact]
        public async Task GetBearerTokenAsync_LoginRemembered_OnlyCallsLocalStorage() {
            var token = "Bar";
            _localStorageServiceMock.Setup(mock => mock.GetAsync<string>("ScrumBoardBearerTokenPurpose", "BEARER_TOKEN")).ReturnsAsync(token);

            var result = await _service.GetBearerTokenAsync();

            _sessionStorageServiceMock.Verify(local => local.GetAsync<string>(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _localStorageServiceMock.Verify(local => local.GetAsync<string>(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            result.Should().Be(token);
        }

        [Fact]
        public void GetBearerTokenAsync_LoginNotRemembered_CallsLocalStorageThenSessionStorage()
        {
            var token = "Bar";
            _localStorageServiceMock.Setup(mock => mock.GetAsync<string>("ScrumBoardBearerTokenPurpose", "BEARER_TOKEN")).ReturnsAsync(() => null);
            _sessionStorageServiceMock.Setup(mock => mock.GetAsync<string>("ScrumBoardBearerTokenPurpose", "BEARER_TOKEN")).ReturnsAsync(token);

            var result = _service.GetBearerTokenAsync().GetAwaiter().GetResult();

            _sessionStorageServiceMock.Verify(local => local.GetAsync<string>(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _localStorageServiceMock.Verify(local => local.GetAsync<string>(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            result.Should().Be(token);
        }

        [Fact]
        public async Task GetBearerTokenAsync_NotLoggedIn_ReturnsNullAfterCallingBothStorages()
        {
            _localStorageServiceMock.Setup(mock => mock.GetAsync<string>("ScrumBoardBearerTokenPurpose", "BEARER_TOKEN")).ReturnsAsync(() => null);
            _sessionStorageServiceMock.Setup(mock => mock.GetAsync<string>("ScrumBoardBearerTokenPurpose", "BEARER_TOKEN")).ReturnsAsync(() => null);

            var result = await _service.GetBearerTokenAsync();

            _sessionStorageServiceMock.Verify(local => local.GetAsync<string>(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _localStorageServiceMock.Verify(local => local.GetAsync<string>(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            result.Should().Be(null);
        }
        
        [Fact]
        public async Task GetBearerTokenAsync_FailsDueToCryptographicException_LogsAndReturnsNull()
        {
            _localStorageServiceMock.Setup(mock => mock.GetAsync<string>("ScrumBoardBearerTokenPurpose", "BEARER_TOKEN")).ThrowsAsync(new CryptographicException());

            var result = await _service.GetBearerTokenAsync();
            
            VerifyLoggerWasCalled();

            result.Should().Be(null);
        }

        [Fact]
        public async Task RemoveBearerTokenAsync_Called_DeletesFromBothUnderlyingStorage() {
            await _service.RemoveBearerTokenAsync();
            _localStorageServiceMock.Verify(mock => mock.DeleteAsync("BEARER_TOKEN"));
            _sessionStorageServiceMock.Verify(mock => mock.DeleteAsync("BEARER_TOKEN"));
        }

        [Fact]
        public async Task SetSelectedProjectIdAsync_WithValue_AddsToLocalStorage() {
            long selectedProject = 6;
            await _service.SetSelectedProjectIdAsync(selectedProject);
            _localStorageServiceMock.Verify(mock => mock.SetAsync("SELECTED_PROJECT_ID", selectedProject));
        }

        [Fact]
        public async Task GetSelectedProjectIdAsync_Called_FetchesFromLocalStorage() {
            long selectedProject = 8;
            _localStorageServiceMock.Setup(mock => mock.GetAsync<long?>("SELECTED_PROJECT_ID")).ReturnsAsync(selectedProject);

            var result = await _service.GetSelectedProjectIdAsync();
            result.Should().Be(selectedProject);
        }

        [Fact]
        public async Task GetSelectedProjectIdAsync_FailsDueToCryptographyException_LogsAndReturnsNull() {
            _localStorageServiceMock.Setup(mock => mock.GetAsync<long?>("SELECTED_PROJECT_ID")).ThrowsAsync(new CryptographicException());

            var result = await _service.GetSelectedProjectIdAsync();
            
            VerifyLoggerWasCalled();
            result.Should().BeNull();
        }
    }
}
