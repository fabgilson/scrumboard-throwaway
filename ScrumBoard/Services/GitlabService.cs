using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Gitlab;
using ScrumBoard.Utils;

namespace ScrumBoard.Services
{
    public interface IGitlabService
    {
        Task TestCredentials(GitlabCredentials credentials);
        Task<List<GitlabBranch>> GetBranches(GitlabCredentials credentials, params string[] attributes);
        Task<List<GitlabCommit>> GetCommits(GitlabCredentials credentials, params string[] attributes);
        Task<GitlabCommit> GetCommit(GitlabCredentials credentials, string sha);
    }

    public static class GitlabApiAttribute
    {
        public static string RefName(string value) => $"ref_name={Uri.EscapeDataString(value)}";
        public static string Since(DateTime date) => $"since={date.ToUniversalTime().ToString("o")}";
        public static string Until(DateTime date) => $"until={date.ToUniversalTime().ToString("o")}";
        public static string PerPage(int value) => $"per_page={value}";
        public static string Page(int value) => $"page={value}";
    }

    public class GitlabService : IGitlabService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ILogger<GitlabService> _logger;

        public GitlabService(
            IHttpClientFactory httpClientFactory,
            ILogger<GitlabService> logger
        ) 
        {   
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private static readonly JsonSerializerOptions options = new() {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        };
        
        private static string ApplyAttributes(string[] attributes, string url)
        {
            int index = 0;

            foreach (var attribute in attributes)
            {
                if (index == 0) url += "?";
                else if (index <= attributes.Length) url += "&";
                url += $"{attribute}";
                index++;
            }

            return url;
        }

        private HttpClient MakeClient(GitlabCredentials credentials) {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = credentials.GitlabURL;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Private-Token", credentials.AccessToken);
            return client;
        }

        private async Task<T> Get<T>(GitlabCredentials credentials, string path) {
            HttpResponseMessage response;
            try {
                response = await MakeClient(credentials).GetAsync("api/v4/" + path);
            } 
            catch (HttpRequestException ex)
            {
                throw new GitlabRequestFailedException(RequestFailure.ConnectionFailed, ex);
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    throw new GitlabRequestFailedException(RequestFailure.NotFound);
                case HttpStatusCode.Forbidden:
                    throw new GitlabRequestFailedException(RequestFailure.Forbidden);
                case HttpStatusCode.Unauthorized:
                    throw new GitlabRequestFailedException(RequestFailure.Unauthorized);
            }

            if (!response.IsSuccessStatusCode) throw new GitlabRequestFailedException(RequestFailure.BadHttpStatus);

            await using var stream = await response.Content.ReadAsStreamAsync();
            try {
                return await JsonSerializer.DeserializeAsync<T>(stream, options);
            } catch (JsonException ex) {
                throw new GitlabRequestFailedException(RequestFailure.InvalidPayload, ex);
            }
        }

        /// <summary>
        /// Gets a GitLab project from some credentials
        /// </summary>
        /// <param name="credentials">Credentials to run request with</param>
        /// <exception cref="GitlabRequestFailedException">Thrown if the request fails</exception>
        /// <returns>GitlabProject belonging to the credentials</returns>
        private async Task<GitlabProject> GetProject(GitlabCredentials credentials)
        {
            return await Get<GitlabProject>(credentials, $"projects/{credentials.Id}");
        }

        /// <summary>
        /// Gets branches within a GitLab project
        /// </summary>
        /// <param name="credentials">Credentials to perform request with</param>
        /// <param name="attributes">List of branch attributes generated from GitlabApiAttribute</param>
        /// <exception cref="GitlabRequestFailedException">Thrown if the request fails</exception>
        /// <returns>Branches in the project matching the attributes</returns>
        public async Task<List<GitlabBranch>> GetBranches(GitlabCredentials credentials, params string[] attributes)
        {
            string url = ApplyAttributes(attributes, $"projects/{credentials.Id}/repository/branches");
            var branches = await Get<List<GitlabBranch>>(credentials, url);
            return branches;
        }

        /// <summary>
        /// Runs a request using the given credentials, if the credentials are invalid an exception will be thrown
        /// </summary>
        /// <param name="credentials">Credentials to test</param>
        /// <exception cref="GitlabRequestFailedException">Thrown if the credentials are invalid</exception>
        public async Task TestCredentials(GitlabCredentials credentials)
        {
            var project = await GetProject(credentials);
            var projectAccess = project?.Permissions?.ProjectAccess;
            if (projectAccess == null) throw new GitlabRequestFailedException(RequestFailure.InvalidPayload);
            if (projectAccess.AccessLevel < 40) throw new GitlabRequestFailedException(RequestFailure.Forbidden);
        }

        /// <summary>
        /// Gets commits within a GitLab project
        /// </summary>
        /// <param name="credentials">Credentials to perform request with</param>
        /// <param name="attributes">List of commit attributes generated from GitlabApiAttribute</param>
        /// <exception cref="GitlabRequestFailedException">Thrown if the request fails</exception>
        /// <returns>Commits in the project matching the attributes</returns>
        public async Task<List<GitlabCommit>> GetCommits(GitlabCredentials credentials, params string[] attributes)
        {
            var url = ApplyAttributes(attributes, $"projects/{credentials.Id}/repository/commits");
            return await Get<List<GitlabCommit>>(credentials, url);
        }

        public async Task<GitlabCommit> GetCommit(GitlabCredentials credentials, string sha)
        {
            var url = $"projects/{credentials.Id}/repository/commits/{sha}";
            return await Get<GitlabCommit>(credentials, url);
        }
    }
}