using System;
using System.Text.Json;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Utils
{
    public class GitlabCredentialsTypeConverter : StringToTypeConverter<GitlabCredentials> 
    {
        protected override GitlabCredentials Decode(string input)
        {
            return JsonSerializer.Deserialize<GitlabCredentials>(input);
        }

        protected override string Encode(GitlabCredentials input)
        {
            return JsonSerializer.Serialize(input);
        }
    }
}