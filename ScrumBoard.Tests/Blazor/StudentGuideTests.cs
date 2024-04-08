using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Bunit;
using Bunit.TestDoubles;
using Castle.DynamicProxy;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.Pages;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Tests.Unit.Utils;
using ScrumBoard.Utils;
using Xunit;
using IInvocation = Castle.DynamicProxy.IInvocation;

namespace ScrumBoard.Tests.Blazor;

internal class StudentGuideServiceInterceptor : IInterceptor
{
    public bool SearchCalled { get; set; }
    public string SearchCalledStringValue { get; set; }
    public int SearchInvocationCount { get; set; }
    
    public void Intercept(IInvocation invocation)
    {
        switch (invocation.Method.Name)
        {
            case nameof(IStudentGuideService.SearchForText):
                SearchCalled = true;
                SearchCalledStringValue = invocation.Arguments.First().As<string>();
                SearchInvocationCount++;
                break;
        }
        invocation.Proceed();
    }
}

public class StudentGuideTests : TestContext
{
    private MockFileSystem _mockFileSystem;
    private Mock<IClock> _clockMock;
    private Mock<IJsInteropService> _jsInteropServiceMock;

    private StudentGuideServiceInterceptor _studentGuideServiceInterceptor;
    
    private static readonly string TestBasePath = Path.GetFullPath("/test/path/");
    private static readonly string TestContentPath = Path.GetFullPath(Path.Join(TestBasePath, "content/"));
    
    private const string TestGitLabTagPath = "https://my-fake-gitlab-instance.com/tags";
    private const string TestGitLabZipPath = "https://my-fake-gitlab-instance.com/archive.zip";
    private const string TestGitLabAccessToken = "access-token-for-tests";
    
    private IRenderedComponent<StudentGuide> CreateComponent(
        IEnumerable<(string, MockFileData)> mockFiles = null,
        IEnumerable<string> mockDirectories = null,
        string requestedFilename = null,
        string textToHighlight = null,
        DateTime? mockedNowDateTime = null
    ) {
        _clockMock = new Mock<IClock>();
        _clockMock.Setup(x => x.Now).Returns(mockedNowDateTime ?? DateTime.Now);

        var configurationMock = new Mock<IConfigurationService>();
        configurationMock.SetupGet(x => x.StudentGuideContentPath).Returns(TestBasePath);
        configurationMock.SetupGet(x => x.StudentGuideGitlabTagPath).Returns(TestGitLabTagPath);
        configurationMock.SetupGet(x => x.StudentGuideGitlabZipPath).Returns(TestGitLabZipPath);
        configurationMock.SetupGet(x => x.StudentGuideGitlabAccessToken).Returns(TestGitLabAccessToken);
        
        var loggerMock = new Mock<ILogger<StudentGuideService>>();
        _jsInteropServiceMock = new Mock<IJsInteropService>();
        _jsInteropServiceMock.Setup(x => x.MarkTextInsideElement(It.IsAny<string>(), It.IsAny<string>()));

        _mockFileSystem = new MockFileSystem();
        foreach (var mockFilePair in mockFiles ?? Array.Empty<(string, MockFileData)>())
        {
            _mockFileSystem.AddFile(mockFilePair.Item1, mockFilePair.Item2);
        }
        foreach (var mockDirectory in mockDirectories ?? Array.Empty<string>())
        {
            _mockFileSystem.AddDirectory(mockDirectory);
        }
        
        Services.AddScoped(_ => _jsInteropServiceMock.Object);
        Services.AddScoped(_ => new Mock<IUsageDataService>().Object);
        Services.AddScoped(_ => configurationMock.Object);
        Services.AddScoped(_ => loggerMock);
        Services.AddScoped<IFileSystem>(_ => _mockFileSystem);

        Services.AddTransient<StudentGuideServiceInterceptor>();
        Services.AddScoped(serviceProvider =>
        {
            _studentGuideServiceInterceptor = serviceProvider.GetRequiredService<StudentGuideServiceInterceptor>();
            var configService = serviceProvider.GetRequiredService<IConfigurationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<StudentGuideService>>();
            var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
            IStudentGuideService target = new StudentGuideService(configService, logger, fileSystem, null);
            return new ProxyGenerator().CreateInterfaceProxyWithTargetInterface(target, _studentGuideServiceInterceptor);
        });
        
        return RenderComponent<StudentGuide>(parameters => parameters
            .Add(p => p.PageName, requestedFilename)
        );
    }

    private static (string, MockFileData) CreateIndexPage(string indexPageContent = null)
    {
        indexPageContent ??= "# Index page";
        return CreateMarkdownPage("index", indexPageContent);
    }
    
    private static (string, MockFileData) CreateMarkdownPage(string filename, string pageContent)
    {
        return (Path.GetFullPath(Path.Join(TestContentPath, filename + ".md")), new MockFileData(pageContent));
    }

    [Fact]
    public void PageLoaded_NoFilenameGivenAndNoStudentGuideContentFound_IndexPageShown()
    {
        var cut = CreateComponent();
        cut.Find("#student-guide-markdown-container").TextContent.Trim().Should().StartWith("Error reading Student Guide content, please try again later.");
    }
    
    [Fact]
    public void PageLoaded_NoFilenameGiven_IndexPageShown()
    {
        var cut = CreateComponent(mockFiles: new []{ CreateIndexPage() });
        cut.Find("#student-guide-markdown-container").TextContent.Trim().Should().Be("Index page");
    }
    
    [Fact]
    public void PageLoaded_NonExistentFilenameGiven_IndexPageShown()
    {
        var cut = CreateComponent(mockFiles: new []{ CreateIndexPage() }, requestedFilename: "notFound");
        cut.Find("#student-guide-markdown-container").TextContent.Trim().Should().Be("Index page");
    }
    
    [Fact]
    public void Searching_SingleCharacterEntered_SearchIsCalledOnceAfterDelay()
    {
        var cut = CreateComponent(mockFiles: new[] { CreateIndexPage() });

        cut.Find("#student-guide-search-input").Input("a");
        AssertionHelper.WaitFor(() => _studentGuideServiceInterceptor.SearchCalled.Should().BeFalse());
        
        // Now move time forwards and check that search task does occur
        _clockMock.Setup(x => x.Now).Returns(DateTime.Now.AddSeconds(10));
        AssertionHelper.WaitFor(() => _studentGuideServiceInterceptor.SearchCalled.Should().BeTrue());
    }
    
    [Fact]
    public void Searching_MultipleCharactersEntered_SearchIsOnlyCalledOnceAfterDelay()
    {
        var cut = CreateComponent(mockFiles: new[] { CreateIndexPage() });

        cut.Find("#student-guide-search-input").Input("a");
        cut.Find("#student-guide-search-input").Input("b");
        cut.Find("#student-guide-search-input").Input("c");
        AssertionHelper.WaitFor(() => _studentGuideServiceInterceptor.SearchCalled.Should().BeFalse());
        
        // Now move time forwards and check that search task does occur
        _clockMock.Setup(x => x.Now).Returns(DateTime.Now.AddSeconds(10));
        AssertionHelper.WaitFor(() => _studentGuideServiceInterceptor.SearchCalled.Should().BeTrue());
        _studentGuideServiceInterceptor.SearchInvocationCount.Should().Be(1);
    }
    
    [Fact]
    public void Searching_MoreThan50CharactersEntered_OnlyFirst50CharactersUsed()
    {
        var cut = CreateComponent(mockFiles: new[] { CreateIndexPage() });

        cut.Find("#student-guide-search-input").Input(new string('a', 60));
        AssertionHelper.WaitFor(() => _studentGuideServiceInterceptor.SearchCalled.Should().BeFalse());
        
        // Now move time forwards and check that search task does occur
        _clockMock.Setup(x => x.Now).Returns(DateTime.Now.AddSeconds(10));
        AssertionHelper.WaitFor(() => _studentGuideServiceInterceptor.SearchCalled.Should().BeTrue());
        _studentGuideServiceInterceptor.SearchInvocationCount.Should().Be(1);
        _studentGuideServiceInterceptor.SearchCalledStringValue.Should().Be(new string('a', 50));
    }

    [Fact]
    public void Searching_SearchQueryGivenWithSomePagesReturned_AllPagesShownInResults()
    {
        var cut = CreateComponent(mockFiles: new[]
        {
            CreateIndexPage(),
            CreateMarkdownPage("example-page-1", "# Example 1\n\nscrum is fun"),
            CreateMarkdownPage("example-page-2", "# Example 2\n\nscrum is really fun\n\nI love scrum"),
        });
        
        cut.Find("#student-guide-search-input").Input("scrum");
        AssertionHelper.WaitFor(() => _studentGuideServiceInterceptor.SearchCalled.Should().BeTrue());

        cut.WaitForElement("#student-guide-found-results-container");
        var childPages = cut.Find("#student-guide-found-results-container").Children;

        childPages.Should().Contain(x => x.TextContent == "example-page-1");
        childPages.Should().Contain(x => x.TextContent == "example-page-2");
    }
    
    [Fact]
    public void Searching_SearchQueryGivenWithTooManyPagesReturned_OnlySomePagesShown()
    {
        var cut = CreateComponent(mockFiles: new[] { CreateIndexPage() }
            .Union(Enumerable.Range(1,20).Select(i => CreateMarkdownPage($"example-page-{i}", $"# Example {i}\n\nscrum is fun")))
        );
        
        cut.Find("#student-guide-search-input").Input("scrum");
        AssertionHelper.WaitFor(() => _studentGuideServiceInterceptor.SearchCalled.Should().BeTrue());

        cut.WaitForElement("#student-guide-found-results-container");
        var children = cut.Find("#student-guide-found-results-container").Children;

        children.Where(x => x.ClassList.Contains("student-guide-search-result-filename")).Should().HaveCount(5);
    }
    
    [Fact]
    public void Searching_SearchQueryGivenWithLotsOfResultsOnOnePageGiven_OnePageWithOnlySomeResultsShown()
    {
        var cut = CreateComponent(mockFiles: new[]
        {
            CreateIndexPage(), 
            CreateMarkdownPage("example-page", "# Example page" + string.Concat(Enumerable.Range(1,20).Select(i => $"{i} scrum\n\n")))
        });
        
        cut.Find("#student-guide-search-input").Input("scrum");
        AssertionHelper.WaitFor(() => _studentGuideServiceInterceptor.SearchCalled.Should().BeTrue());

        cut.WaitForElement("#student-guide-found-results-container");
        var children = cut.Find("#student-guide-found-results-container").Children;
        var childPages = children.Where(x => x.ClassList.Contains("student-guide-search-result-filename"));
        var matchingLines = children.Where(x => x.ClassList.Contains("student-guide-search-result-line"));
            
        childPages.Should().HaveCount(1);
        matchingLines.Should().HaveCount(5);
    }

    [Fact]
    public async Task SearchResultsShowing_SearchResultClicked_CorrectNavigationOccurs()
    {
        var cut = CreateComponent(mockFiles: new[]
        {
            CreateIndexPage(),
            CreateMarkdownPage("example-page-1", "# Example 1\n\nscrum is fun")
        });
        var fakeNavigationManager = Services.GetRequiredService<FakeNavigationManager>();
        
        cut.Find("#student-guide-search-input").Input("scrum");

        // Wait for search dropdown to fully open, we know animation has finished when opacity is 1
        cut.WaitForAssertion(() => cut.Find("#student-guide-found-results-container")
            .ComputeCurrentStyle().GetOpacity().Should().BeOneOf("", "1"));
        
        var searchLink = cut.WaitForElement("#student-guide-found-results-container").Children
            .Single(x => x.ClassList.Contains("student-guide-search-result-line"));
        await searchLink.ClickAsync(new MouseEventArgs());
        
        cut.WaitForAssertion(() => fakeNavigationManager.History.Should().Contain(x => x.Uri == "./student-guide/example-page-1"));
        _jsInteropServiceMock.Verify(x => x.MarkTextInsideElement("student-guide-markdown-container", "scrum is fun"));
    }
}