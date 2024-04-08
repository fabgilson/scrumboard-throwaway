using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace ScrumBoard.Models.Entities
{
    [Owned]
    public class GitlabCredentials {
        
        [Column(TypeName = "varchar(95)")]
        public Uri GitlabURL { get; private set; }
        public long Id { get; private set; }
        public string AccessToken { get; private set; }
        public string PushWebhookSecretToken { get; private set; }
        
        public GitlabCredentials() {}

        [JsonConstructor]
        public GitlabCredentials(Uri gitlabURL, long id, string accessToken, string pushWebhookSecretToken) {
            GitlabURL = gitlabURL;
            Id = id;
            AccessToken = accessToken;
            PushWebhookSecretToken = pushWebhookSecretToken;
        }

        public override bool Equals(object obj)
        {
            if (obj is GitlabCredentials other) {
                return (GitlabURL, Id, AccessToken, PushWebhookSecretToken) == (other.GitlabURL, other.Id, other.AccessToken, other.PushWebhookSecretToken);
            } else {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return (GitlabURL, Id, AccessToken, PushWebhookSecretToken).GetHashCode();
        }

        public override string ToString()
        {
            return $"ProjectId={Id}, GitlabURL={GitlabURL}";
        }
    }
}