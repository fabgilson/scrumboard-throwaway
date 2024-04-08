using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ScrumBoard.Pages;
using ScrumBoard.Services;
using Xunit;

namespace ScrumBoard.Tests.Unit.Services;

public class StudentGuideServiceUnderTest : StudentGuideService
{
    public StudentGuideServiceUnderTest(
        IConfigurationService configService,
        ILogger<StudentGuideServiceUnderTest> logger,
        IFileSystem fileSystem,
        HttpClient httpClientMock) : base(configService, logger, fileSystem, null)
    {
        HttpClient = httpClientMock;
    }

    protected override HttpClient HttpClient { get; }
}

public class StudentGuideServiceTest
{
    private StudentGuideServiceUnderTest _studentGuideService;

    private static readonly string TestBasePath = Path.GetFullPath("/test/path/");
    private static readonly string TestContentPath = Path.GetFullPath(Path.Join(TestBasePath, "content/"));
    private static readonly string TestMediaPath = Path.GetFullPath(Path.Join("wwwroot/", PageRoutes.StudentGuideMediaFolder));
    
    private const string TestGitLabTagPath = "https://my-fake-gitlab-instance.com/tags";
    private const string TestGitLabZipPath = "https://my-fake-gitlab-instance.com/archive.zip";
    private const string TestGitLabAccessToken = "access-token-for-tests";

    private MockFileSystem _mockFileSystem;
    
    private void SetupStudentGuideService(
        IEnumerable<(string, MockFileData)> mockFiles = null,
        IEnumerable<string> mockDirectories = null,
        IConfigurationService customConfigService = null,
        HttpClient customHttpClient = null
    ) {
        IConfigurationService configService;
        if (customConfigService is null)
        {
            var configurationMock = new Mock<IConfigurationService>();
            configurationMock.SetupGet(x => x.StudentGuideContentPath).Returns(TestBasePath);
            configurationMock.SetupGet(x => x.StudentGuideGitlabTagPath).Returns(TestGitLabTagPath);
            configurationMock.SetupGet(x => x.StudentGuideGitlabZipPath).Returns(TestGitLabZipPath);
            configurationMock.SetupGet(x => x.StudentGuideGitlabAccessToken).Returns(TestGitLabAccessToken);
            configService = configurationMock.Object;
        }
        else
        {
            configService = customConfigService;
        }
        
        var loggerMock = new Mock<ILogger<StudentGuideServiceUnderTest>>();
        
        _mockFileSystem = new MockFileSystem();
        foreach (var mockFilePair in mockFiles ?? Array.Empty<(string, MockFileData)>())
        {
            _mockFileSystem.AddFile(mockFilePair.Item1, mockFilePair.Item2);
        }
        foreach (var mockDirectory in mockDirectories ?? Array.Empty<string>())
        {
            _mockFileSystem.AddDirectory(mockDirectory);
        }
        
        _studentGuideService = new StudentGuideServiceUnderTest(configService, loggerMock.Object, _mockFileSystem, customHttpClient);
    }
    
    private static HttpClient SetupMockHttpClient(
        string gitLabTagResponseContent = null, 
        byte[] gitLabZipArchiveResponseContent = null,
        HttpStatusCode gitLabStatusCode = HttpStatusCode.OK
    ) {
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) => new HttpResponseMessage
            {
                StatusCode = gitLabStatusCode, 
                Content = request.RequestUri!.AbsoluteUri.Contains(TestGitLabTagPath)
                    ? new StringContent(gitLabTagResponseContent!)
                    : new ByteArrayContent(gitLabZipArchiveResponseContent!)
            });
        
        return new HttpClient(httpMessageHandlerMock.Object);
    }

    [Fact]
    private async Task GetMarkdownContent_ContentRootNotConfigured_InvalidOperationExceptionThrown()
    {
        SetupStudentGuideService(customConfigService: new Mock<IConfigurationService>().Object);
        var action = async () => await _studentGuideService.GetMarkdownContentAsync("file");
        await action.Should().ThrowExactlyAsync<InvalidOperationException>();
    }
    
    [Fact]
    private async Task GetMarkdownContent_ContentRootConfiguredButNoSuchDirectory_DirectoryNotFoundExceptionThrown()
    {
        SetupStudentGuideService();
        var action = async () => await _studentGuideService.GetMarkdownContentAsync("file");
        await action.Should().ThrowExactlyAsync<DirectoryNotFoundException>();
    }
    
    [Fact]
    private async Task GetMarkdownContent_ContentRootConfiguredAndDirectoryExistsButFileDoesNotExist_FileNotFoundExceptionThrown()
    {
        SetupStudentGuideService(mockDirectories: new [] { TestContentPath });
        var action = async () => await _studentGuideService.GetMarkdownContentAsync("notfound");
        await action.Should().ThrowExactlyAsync<FileNotFoundException>();
    }
    
    [Fact]
    private async Task GetMarkdownContent_NamedFileDoesExist_ContentReturned()
    {
        SetupStudentGuideService(
            mockFiles: new[] { (Path.GetFullPath(Path.Join(TestContentPath, "is_found.md")), new MockFileData("Some content")) },
            mockDirectories: new [] { TestContentPath }
        );
        var result = await _studentGuideService.GetMarkdownContentAsync("is_found");
        result.Should().Be("Some content");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    private async Task GetMarkdownContent_NoFilenameGiven_IndexContentReturned(string requestedFile)
    {
        SetupStudentGuideService(
            mockFiles: new[] { (Path.GetFullPath(Path.Join(TestContentPath, "/index.md")), new MockFileData("Some content")) },
            mockDirectories: new [] { TestContentPath }
        );
        var result = await _studentGuideService.GetMarkdownContentAsync(requestedFile);
        result.Should().Be("Some content");
    }
    
    [Fact]
    private async Task CheckForUpdates_NoGitLabTagsAvailable_NewlyAvailableVersionIsNull()
    {
        SetupStudentGuideService(
            mockDirectories: new [] { TestBasePath },
            customHttpClient: SetupMockHttpClient(gitLabTagResponseContent: "[]")
        );

        var versionCheck = await _studentGuideService.CheckForUpdateAsync();
        versionCheck.NewlyAvailableVersion.Should().BeNull();
    }
    
    [Fact]
    private async Task CheckForUpdates_GitLabTagsAreAvailable_NewlyAvailableVersionIsCorrect()
    {
        SetupStudentGuideService(
            mockDirectories: new [] { TestBasePath },
            customHttpClient: SetupMockHttpClient(
                gitLabTagResponseContent: @"[{""name"": ""test-tag"", ""target"": ""abc123""}]"
            )
        );

        var versionCheck = await _studentGuideService.CheckForUpdateAsync();
        versionCheck.NewlyAvailableVersion.Should().BeEquivalentTo(new StudentGuideVersion
        {
            Version = "test-tag",
            GitHash = "abc123",
            LastUpdated = null
        });
    }

    private static byte[] CreateZipFileByteArray(params string[] fileEntries)
    {
        var memoryStream = new MemoryStream();
        using var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
        foreach (var filename in fileEntries) zipArchive.CreateEntry(filename);
        zipArchive.Dispose();
        return memoryStream.ToArray();
    }
    
    [Fact]
    private async Task UpdateStudentGuide_ArchiveIsAvailable_SuccessfullyExtracted()
    {
        SetupStudentGuideService(
            mockDirectories: new [] { TestBasePath },
            customHttpClient: SetupMockHttpClient(
                gitLabTagResponseContent: @"[{""name"": ""test-tag"", ""target"": ""abc123""}]",
                gitLabZipArchiveResponseContent: CreateZipFileByteArray(
                    "parts/index.md",
                    "parts/content-1.md",
                    "imgs/my-image.jpeg",
                    "some-other-junk.md",
                    "junk/some-more-junk.md"
                ))
        );
        
        await _studentGuideService.UpdateStudentGuideAsync();
        using (new AssertionScope())
        {
            _mockFileSystem.AllFiles.Should().Contain(TestBasePath + "latest-archive.zip");
            _mockFileSystem.AllFiles.Should().Contain(TestBasePath + ".student-guide-version");
            _mockFileSystem.AllFiles.Should().Contain(TestContentPath + "index.md");
            _mockFileSystem.AllFiles.Should().Contain(TestContentPath + "content-1.md");
            _mockFileSystem.AllFiles.Should().ContainMatch("*" + TestMediaPath + "my-image.jpeg");
            _mockFileSystem.AllFiles.Count().Should().Be(5);
        }
    }
    
    [Fact]
    private async Task UpdateStudentGuide_ArchiveIsAvailable_VersionFileWrittenCorrectly()
    {
        SetupStudentGuideService(
            mockDirectories: new [] { TestBasePath },
            customHttpClient: SetupMockHttpClient(
                gitLabTagResponseContent: @"[{""name"": ""test-tag"", ""target"": ""abc123""}]",
                gitLabZipArchiveResponseContent: CreateZipFileByteArray()
            )
        );
        
        await _studentGuideService.UpdateStudentGuideAsync();

        var versionFileText = await _mockFileSystem.File.ReadAllTextAsync(TestBasePath + "/.student-guide-version");
        var version = JsonSerializer.Deserialize<StudentGuideVersion>(versionFileText);
        using (new AssertionScope())
        {
            version.Version.Should().Be("test-tag");
            version.GitHash.Should().Be("abc123");
            version.LastUpdated.Should()
                .NotBeNull().And
                .BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }
    }
    
    [Fact]
    private async Task UpdateStudentGuide_NoArchiveIsAvailable_InvalidOperationExceptionThrown()
    {
        SetupStudentGuideService(
            mockDirectories: new [] { TestBasePath },
            customHttpClient: SetupMockHttpClient(
                gitLabTagResponseContent: @"[]"
            )
        );
        
        var action = async () => await _studentGuideService.UpdateStudentGuideAsync();
        await action.Should().ThrowExactlyAsync<InvalidOperationException>();
    }
    
    [Fact]
    private async Task UpdateStudentGuide_LocalContentAlreadyExists_ExistingContentReplaced()
    {
        // First data download
        SetupStudentGuideService(
            mockDirectories: new [] { TestBasePath },
            customHttpClient: SetupMockHttpClient(
                gitLabTagResponseContent: @"[{""name"": ""test-tag"", ""target"": ""abc123""}]",
                gitLabZipArchiveResponseContent: CreateZipFileByteArray(
                    "parts/index.md",
                    "parts/content-1.md",
                    "imgs/my-image-1.jpeg"
                ))
        );
        await _studentGuideService.UpdateStudentGuideAsync();
        
        // Now a new version to replace it with
        SetupStudentGuideService(
            mockDirectories: new[] { TestBasePath },
            customHttpClient: SetupMockHttpClient(
                gitLabTagResponseContent: @"[{""name"": ""test-tag-v2"", ""target"": ""abc456""}]",
                gitLabZipArchiveResponseContent: CreateZipFileByteArray(
                    "parts/index.md",
                    "parts/content-2.md",
                    "imgs/my-image-2.jpeg"
                ))
        );
        await _studentGuideService.UpdateStudentGuideAsync();

        using (new AssertionScope())
        {
            _mockFileSystem.AllFiles.Should().Contain(TestContentPath + "index.md");
            _mockFileSystem.AllFiles.Should().NotContain(TestContentPath + "content-1.md");
            _mockFileSystem.AllFiles.Should().Contain(TestContentPath + "content-2.md");
            _mockFileSystem.AllFiles.Should().NotContainMatch("*" + TestMediaPath + "my-image-1.jpeg");
            _mockFileSystem.AllFiles.Should().ContainMatch("*" + TestMediaPath + "my-image-2.jpeg");
            _mockFileSystem.AllFiles.Count().Should().Be(5); // 3 files from archive, .zip, and .version file
        }
    }
    
    [Theory]
    [InlineData(nameof(IConfigurationService.StudentGuideContentPath))]
    [InlineData(nameof(IConfigurationService.StudentGuideGitlabTagPath))]
    [InlineData(nameof(IConfigurationService.StudentGuideGitlabZipPath))]
    private async Task ValidateConfiguration_MissingConfigVariables_ExpectedErrorsReturned(string missingConfigVarName)
    {
        var configurationMock = new Mock<IConfigurationService>();
        if(missingConfigVarName != nameof(IConfigurationService.StudentGuideContentPath))
            configurationMock.SetupGet(x => x.StudentGuideContentPath).Returns(TestBasePath);
        if(missingConfigVarName != nameof(IConfigurationService.StudentGuideGitlabTagPath))
            configurationMock.SetupGet(x => x.StudentGuideGitlabTagPath).Returns(TestGitLabTagPath);
        if(missingConfigVarName != nameof(IConfigurationService.StudentGuideGitlabZipPath))
            configurationMock.SetupGet(x => x.StudentGuideGitlabZipPath).Returns(TestGitLabZipPath);
        SetupStudentGuideService(customConfigService: configurationMock.Object);

        var validationReport = await _studentGuideService.ValidateConfigurationAsync();
        validationReport.Success.Should().BeFalse();
        validationReport.ValidationErrors.Should().ContainSingle(e => e.MemberName == missingConfigVarName);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    private async Task ValidateConfiguration_GitLabTagRequestFails_CorrectErrorReturned(HttpStatusCode failingCode)
    {
        SetupStudentGuideService(customHttpClient: SetupMockHttpClient(gitLabStatusCode: failingCode, gitLabTagResponseContent: "{}"));
        var validationReport = await _studentGuideService.ValidateConfigurationAsync();
        validationReport.Success.Should().BeFalse();
        validationReport.ValidationErrors.Should().ContainSingle(e => e.MemberName == nameof(IConfigurationService.StudentGuideGitlabTagPath));
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("[{}]")]
    private async Task ValidateConfiguration_GitLabTagRequestReturnsUnexpectedJson_CorrectErrorReturned(string unexpectedJson)
    {
        SetupStudentGuideService(customHttpClient: SetupMockHttpClient(gitLabTagResponseContent: unexpectedJson));
        var validationReport = await _studentGuideService.ValidateConfigurationAsync();
        validationReport.Success.Should().BeFalse();
        validationReport.ValidationErrors.Should().ContainSingle(e => e.MemberName == nameof(IConfigurationService.StudentGuideGitlabTagPath));
    }
}