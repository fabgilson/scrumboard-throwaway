using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FuzzySharp;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models;
using ScrumBoard.Pages;
using ScrumBoard.Utils;

namespace ScrumBoard.Services;

public interface IStudentGuideService
{
    /// <summary>
    /// For some filename, find the corresponding markdown content and return it as a string.
    /// </summary>
    /// <param name="filename">Name of the requested file to read, defaults to "index" if not given</param>
    /// <returns>String contents of requested markdown file</returns>
    /// <exception cref="IOException">
    /// Could not find or read the requested file, either it doesn't exist, or the application does not have the
    /// required privileges to read it.
    /// </exception>
    public Task<string> GetMarkdownContentAsync(string filename);
    
    /// <summary>
    /// Updates the local version of the student guide to the most recent tagged commit in the configured GitLab repo.
    /// </summary>
    /// <exception cref="InvalidOperationException">Attempted to update when no updates are available</exception>
    /// <exception cref="IOException">An issue was encountered in trying to read or write information about current SG version</exception>
    /// <exception cref="JsonException">GitLab API returned malformed JSON data</exception>
    /// <exception cref="KeyNotFoundException">GitLab API returned an unexpected message, likely due to invalid permissions</exception>
    public Task UpdateStudentGuideAsync();
    
    /// <summary>
    /// Checks to see if a new version of the student guide is ready to be downloaded, and also returns information
    /// about what current version (if any) is present.
    /// </summary>
    /// <returns>
    /// Object containing information about what local version of the student guide is present, and what remote version
    /// is available.
    /// </returns>
    /// <exception cref="IOException">An issue was encountered in trying to read information about current SG version</exception>
    /// <exception cref="JsonException">GitLab API returned malformed JSON data</exception>
    /// <exception cref="KeyNotFoundException">GitLab API returned an unexpected message, likely due to invalid permissions</exception>
    public Task<StudentGuideUpdateCheck> CheckForUpdateAsync();

    /// <summary>
    /// Runs some preliminary checks to determine if the Student Guide service has valid configuration, and generates
    /// a basic report with any errors encountered.
    /// </summary>
    /// <returns>
    /// BasicValidationReport containing any errors encountered. The report containing no errors is no guarantee that
    /// the service will execute without issue, but it can offer some confidence that things appear to be configured
    /// correctly.
    /// </returns>
    Task<BasicValidationReport> ValidateConfigurationAsync();

    Task<IOrderedEnumerable<StudentGuideSearchResponse>> SearchForText(string searchText);
}

public struct StudentGuideSearchResponse
{
    /// <summary>
    /// The original text of the line where matches were found
    /// </summary>
    public string OriginalText { get; set; }

    /// <summary>
    /// A list of tuples, each containing the starting index, length of a match, and score of that match
    /// </summary>
    public List<Tuple<int, int, int>> Matches { get; set; }

    /// <summary>
    /// The URL of the file that the match was found in
    /// </summary>
    public string Url { get; set; }
    
    /// <summary>
    /// The total score of all matches in the line
    /// </summary>
    public int TotalScore { get; set; }

    /// <summary>
    /// User friendly filename to display alongside search result
    /// </summary>
    public string FileName { get; set; }
}


internal struct StudentGuideVersionRequestAttempt
{
    public StudentGuideVersion? StudentGuideVersion { get; set; }
    public bool IsGitLabRequestError { get; set; }
    public HttpStatusCode? GitLabStatusCode { get; set; }
    public bool IsDeserializationError { get; set; }
}

public struct StudentGuideVersion
{
    public string Version { get; set; }
    public string GitHash { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public struct StudentGuideUpdateCheck
{
    public StudentGuideVersion? CurrentVersion { get; init; }
    public bool UpdateAvailable { get; init; }
    public StudentGuideVersion? NewlyAvailableVersion { get; init; }
}

public class StudentGuideService : IStudentGuideService
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<StudentGuideService> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IHttpClientFactory _httpClientFactory;
    
    private string MarkdownContentDirectory => Path.GetFullPath(Path.Join(GetContentRootFolder(), "content/"));
    private string VersionFileName => Path.GetFullPath(Path.Join(GetContentRootFolder(), "/.student-guide-version"));
    private string ZipFileName => Path.GetFullPath(Path.Join(GetContentRootFolder(), "/latest-archive.zip"));
    private static string MediaDirectory => Path.GetFullPath(Path.Join("wwwroot/", PageRoutes.StudentGuideMediaFolder));
    
    protected virtual HttpClient HttpClient
    {
        get
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", _configService.StudentGuideGitlabAccessToken);
            return client;
        }
    }
    
    public StudentGuideService(
        IConfigurationService configService, 
        ILogger<StudentGuideService> logger, 
        IFileSystem fileSystem, 
        IHttpClientFactory httpClientFactory
    ) {
        _configService = configService;
        _logger = logger;
        _fileSystem = fileSystem;
        _httpClientFactory = httpClientFactory;
    }
    
    private string GetContentRootFolder(bool throwIfDirectoryNotExists=true)
    {
        var filePath = _configService.StudentGuideContentPath;
        if (filePath is null)
        {
            _logger.LogError("Attempted to get student guide content root folder, but no matching configuration variable was found");
            throw new InvalidOperationException("No configuration value provided for student guide content path");
        }

        var absolutePath = _fileSystem.Path.GetFullPath(filePath);
        if (!_fileSystem.Directory.Exists(filePath) && throwIfDirectoryNotExists)
        {
            _logger.LogError("No directory found at configured student guide content path: {Path}", absolutePath);
            throw new DirectoryNotFoundException("No directory found at configured student guide content path");
        }

        _logger.LogTrace("Student Guide content root path found: {Path}", absolutePath);
        return absolutePath;
    }

    private StudentGuideVersion? ReadVersionFile()
    {
        try
        {
            return JsonSerializer.Deserialize<StudentGuideVersion>(_fileSystem.File.ReadAllText(VersionFileName));
        }
        catch (Exception e) when (e is IOException or JsonException)
        {
            return null;
        }
    }

    private async Task<StudentGuideVersionRequestAttempt> GetMostRecentTagFromGitlabAsync()
    {
        HttpResponseMessage result;
        var requestAttempt = new StudentGuideVersionRequestAttempt();
        try {
            result = await HttpClient.GetAsync(_configService.StudentGuideGitlabTagPath);
        } catch {
            requestAttempt.IsGitLabRequestError = true;
            return requestAttempt;
        }
        if (!result.IsSuccessStatusCode)
        {
            requestAttempt.GitLabStatusCode = result.StatusCode;
            requestAttempt.IsGitLabRequestError = true;
            return requestAttempt;
        }
        
        try
        {
            var content = await result.Content.ReadFromJsonAsync<JsonElement>();
            if (content.GetArrayLength() != 0)
            {
                requestAttempt.StudentGuideVersion = new StudentGuideVersion
                {
                    Version = content[0].GetProperty("name").ToString(),
                    GitHash = content[0].GetProperty("target").ToString(),
                };
            }
        }
        catch
        {
            requestAttempt.IsDeserializationError = true;
        }

        return requestAttempt;
    }

    private async Task DownloadStudentGuideZipArchive(string latestAvailableVersion)
    {
        using var response = await HttpClient.GetAsync(
            _configService.StudentGuideGitlabZipPath
            + $"?sha={latestAvailableVersion}"
        );
        
        // Ensure that the root directory exists
        _fileSystem.Directory.CreateDirectory(GetContentRootFolder(false));
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file = _fileSystem.File.Open(ZipFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        await stream.CopyToAsync(file);
    }

    private async Task ExtractStudentGuideZipFile()
    {
        await using var zipArchiveStream = _fileSystem.FileStream.New(ZipFileName, FileMode.Open);
        using var zipArchive = new ZipArchive(zipArchiveStream, ZipArchiveMode.Read);

        // Delete any existing content, and create the content subdirectory
        if(_fileSystem.Directory.Exists(MarkdownContentDirectory)) _fileSystem.Directory.Delete(MarkdownContentDirectory, true);
        _fileSystem.Directory.CreateDirectory(MarkdownContentDirectory);
        
        // Delete any existing images, and create the images subdirectory
        if(_fileSystem.Directory.Exists(MediaDirectory)) _fileSystem.Directory.Delete(MediaDirectory, true);
        _fileSystem.Directory.CreateDirectory(MediaDirectory);
        
        foreach (var entry in zipArchive.Entries)
        {
            var pathParts = entry.FullName.Split("/");
            if (pathParts.Length < 2 || entry.Name.Equals("")) continue;

            var filename = pathParts[^2] switch
            {
                "parts" => MarkdownContentDirectory + entry.Name,
                "imgs" => MediaDirectory + entry.Name,
                _ => null
            };
            if (filename is null) continue;
            
            await using var file = _fileSystem.File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            await using var stream = entry.Open();
            await stream.CopyToAsync(file);
        }
    }
    /// <inheritdoc />
    public async Task<string> GetMarkdownContentAsync(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) filename = "index";
        var pathToFile = Path.Join(MarkdownContentDirectory, filename + ".md");
        if(!_fileSystem.FileExistsInDirectory(Path.GetFullPath(pathToFile), Path.GetFullPath(MarkdownContentDirectory)))
        {
            _logger.LogInformation("Possible attempted path traversal detected when accessing student guide content, showing index page instead");
            pathToFile = Path.Join(MarkdownContentDirectory, "index.md");
        }
        _logger.LogTrace("Attempting to read markdown content from: {Path}", pathToFile);
        return await _fileSystem.File.ReadAllTextAsync(pathToFile);
    }
    
    /// <inheritdoc />
    public async Task<StudentGuideUpdateCheck> CheckForUpdateAsync()
    {
        var currentVersion = ReadVersionFile();
        var latestAvailableVersion = await GetMostRecentTagFromGitlabAsync();
        var newVersionAvailable = latestAvailableVersion.StudentGuideVersion is not null && (
            !currentVersion.HasValue || currentVersion.Value.Version != latestAvailableVersion.StudentGuideVersion.Value.Version
        );

        return new StudentGuideUpdateCheck
        {
            CurrentVersion = currentVersion,
            UpdateAvailable = newVersionAvailable,
            NewlyAvailableVersion = newVersionAvailable ? latestAvailableVersion.StudentGuideVersion : null
        };
    }

    /// <inheritdoc />
    public async Task<BasicValidationReport> ValidateConfigurationAsync()
    {
        var validationReport = new BasicValidationReport();
        
        // Check that required configuration values have been set
        foreach (var selectorNameTuple in new (Func<IConfigurationService, string>, string)[] { 
            (x => x.StudentGuideContentPath, nameof(IConfigurationService.StudentGuideContentPath)),
            (x => x.StudentGuideGitlabAccessToken, nameof(IConfigurationService.StudentGuideGitlabAccessToken)),
            (x => x.StudentGuideGitlabTagPath, nameof(IConfigurationService.StudentGuideGitlabTagPath)),
            (x => x.StudentGuideGitlabZipPath, nameof(IConfigurationService.StudentGuideGitlabZipPath)) })
        {
            
            if (string.IsNullOrWhiteSpace(selectorNameTuple.Item1(_configService)))
            {
                validationReport.AddValidationError(
                    selectorNameTuple.Item2,
                    "Required configuration property not assigned"
                );
            }
        }
        if (!validationReport.Success) return validationReport;
        
        var gitLabVersionCheckAttempt = await GetMostRecentTagFromGitlabAsync();
        if (gitLabVersionCheckAttempt.IsGitLabRequestError || gitLabVersionCheckAttempt.IsDeserializationError)
        {
            var makeGitlabRequestErrorMessage = (HttpStatusCode? status) => status is null 
                ? "GitLab API could not be reached, is the configured tag URL valid and the GitLab instance reachable?" 
                : $"GitLab API returned failing status code: {status}";
            validationReport.AddValidationError(
                nameof(IConfigurationService.StudentGuideGitlabTagPath),
                gitLabVersionCheckAttempt.IsGitLabRequestError 
                    ? makeGitlabRequestErrorMessage(gitLabVersionCheckAttempt.GitLabStatusCode)
                    : "Failed to deserialize JSON response from GitLab API"
            );
        }

        return validationReport;
    }

    /// <inheritdoc />
    public async Task<IOrderedEnumerable<StudentGuideSearchResponse>> SearchForText(string searchText)
    {
        const int maxSearchableTokens = 5;
        var searchWords = searchText.Split(' ').Take(maxSearchableTokens).ToArray();
        
        var responses = new List<StudentGuideSearchResponse>();

        foreach (var file in _fileSystem.Directory.EnumerateFiles(MarkdownContentDirectory, "*.md"))
        {
            var fileMatches = await FindMatchesInFile(file, searchWords);
            responses.AddRange(fileMatches);
        }

        return responses.OrderByDescending(x => x.TotalScore);
    }

    /// <summary>
    /// Searches the given file for lines that contain matches to the search words.
    /// </summary>
    /// <param name="filePath">Path of the file to search.</param>
    /// <param name="searchWords">Array of words to search for.</param>
    /// <returns>A list of <see cref="StudentGuideSearchResponse"/> containing the matches found in the file.</returns>
    private async Task<IEnumerable<StudentGuideSearchResponse>> FindMatchesInFile(string filePath, string[] searchWords)
    {
        var responses = new List<StudentGuideSearchResponse>();
        using var reader = new StreamReader(_fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read));

        while (await reader.ReadLineAsync() is { } line)
        {
            var plainTextLine = Markdig.Markdown.ToPlainText(line);
            var matchIndices = ExtractMatchIndices(plainTextLine, searchWords);
            if (!matchIndices.Any()) continue;
                
            var response = new StudentGuideSearchResponse
            {
                OriginalText = plainTextLine,
                Matches = matchIndices,
                FileName = Path.GetFileNameWithoutExtension(filePath),
                Url = Path.GetFileNameWithoutExtension(filePath),
                TotalScore = matchIndices.Sum(x => x.Item3)
            };

            responses.Add(response);
        }

        return responses;
    }

    /// <summary>
    /// Extracts match indices for the provided search words within a text line.
    /// </summary>
    /// <param name="textLine">The line of text to search within.</param>
    /// <param name="searchWords">Array of words to search for.</param>
    /// <returns>A list of Tuples where each Tuple contains the start index, length, and score of the match.</returns>
    private static List<Tuple<int, int, int>> ExtractMatchIndices(string textLine, IEnumerable<string> searchWords)
    {
        var words = textLine.Split(' ');
        var matchIndices = new List<Tuple<int, int, int>>();

        foreach (var query in searchWords)
        {
            var matches = Process.ExtractAll(query, words, scorer: new DefaultRatioScorer());
            foreach (var match in matches)
            {
                if (match.Score <= 75) continue;

                var index = textLine.IndexOf(words[match.Index], StringComparison.OrdinalIgnoreCase);
                if (index >= 0 && !matchIndices.Any(m => Overlaps(m, index, words[match.Index].Length)))
                {
                    matchIndices.Add(new Tuple<int, int, int>(index, words[match.Index].Length, match.Score));
                }
            }
        }

        // Remove overlapping matches, keeping the highest scoring match
        return matchIndices.OrderByDescending(m => m.Item3).Where(m => 
            !matchIndices.Any(existingMatch => Overlaps(existingMatch, m.Item1, m.Item2) && m.Item3 < existingMatch.Item3)).ToList();
    }

    /// <summary>
    /// Checks if a given match index range overlaps with an existing match's index range.
    /// </summary>
    /// <param name="existingMatch">The existing match Tuple containing the start index, length, and score.</param>
    /// <param name="index">The start index of the match to check.</param>
    /// <param name="length">The length of the match to check.</param>
    /// <returns>True if the two match index ranges overlap, otherwise false.</returns>
    private static bool Overlaps(Tuple<int, int, int> existingMatch, int index, int length)
    {
        return existingMatch.Item1 <= index && existingMatch.Item1 + existingMatch.Item2 >= index ||
               existingMatch.Item1 <= index + length && existingMatch.Item1 + existingMatch.Item2 >= index + length;
    }

    /// <inheritdoc />
    public async Task UpdateStudentGuideAsync()
    {
        var updateCheck = await CheckForUpdateAsync();
        if (!updateCheck.UpdateAvailable || updateCheck.NewlyAvailableVersion is null) 
            throw new InvalidOperationException("No updates available");
        
        await DownloadStudentGuideZipArchive(updateCheck.NewlyAvailableVersion.Value.Version);
        await ExtractStudentGuideZipFile();

        var newVersion = updateCheck.NewlyAvailableVersion.Value;
        newVersion.LastUpdated = DateTime.Now;
        await _fileSystem.File.WriteAllTextAsync(VersionFileName, JsonSerializer.Serialize(newVersion));
    }
}