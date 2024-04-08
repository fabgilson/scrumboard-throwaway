using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Shapes;
using ScrumBoard.Services;
using ScrumBoard.Utils;
using ScrumBoard.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ScrumBoard.Models.Forms
{
    public class ProjectEditForm: IProjectShape
    {
        private readonly DateOnly _earliestDate;
        private readonly bool _webhooksEnabled;
        public ProjectEditForm(Project project, bool webhooksEnabled) {
            var today = DateOnly.FromDateTime(DateTime.Now);            
            _earliestDate = project.StartDate < today ? project.StartDate : today;
            
            Name = project.Name;
            StartDate = project.StartDate;
            EndDateOptional = project.EndDate;
            Description = project.Description;
            MemberAssociations = project.MemberAssociations.ToList();
            GitlabCredentials = project.GitlabCredentials;
            _webhooksEnabled = webhooksEnabled;
        }
        
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name is required")]
        [MaxLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Name cannot only contain numbers or special characters")]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Description is required")]
        [MaxLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Description cannot only contain numbers or special characters")]
        public string Description { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Start date is required")]        
        [DateWithinTwoYears(ErrorMessage = "Start date must be within the next two years")]  
        [CustomValidation(typeof(ProjectEditForm), "ValidateStartDate")]      
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required(AllowEmptyStrings = false, ErrorMessage = "End date is required")]
        [CustomValidation(typeof(ProjectEditForm), "ValidateEndDate")]
        public DateOnly? EndDateOptional { get; set; }

        [ValidateComplexType]
        public GitlabCredentialsForm GitlabCredentialsForm { get; set; }
        
        public DateOnly EndDate {
            get => EndDateOptional.Value;
            set => throw new InvalidOperationException("Cannot set ProjectEditForm.EndDate directly, use EndDateOptional");
        }

        public List<ProjectUserMembership> MemberAssociations { get; set; } = new();
        public GitlabCredentials GitlabCredentials { 
            get => GitlabCredentialsForm?.GetCredentials(); 
            set => GitlabCredentialsForm = value != null ? new GitlabCredentialsForm(value, _webhooksEnabled) : null; 
        }

        public static ValidationResult ValidateEndDate(DateOnly? endDate, ValidationContext context) {
            var projectEditForm = context.ObjectInstance as ProjectEditForm;

            if (endDate != null && endDate <= projectEditForm.StartDate) {
                return new ValidationResult("End date cannot be before start date");
            }
            return ValidationResult.Success;
        }

        public static ValidationResult ValidateStartDate(DateOnly? startDate, ValidationContext context) {
            var projectEditForm = context.ObjectInstance as ProjectEditForm;

            if (startDate != null && startDate < projectEditForm._earliestDate) {
                return new ValidationResult("Start date cannot be moved earlier than original start date");
            }
            return ValidationResult.Success;
        }

        public List<ProjectChangelogEntry> ApplyChanges(User actingUser, Project project) {
            var changes = new List<ProjectChangelogEntry>();
            changes.AddRange(ShapeUtils.ApplyChanges<IProjectShape>(this, project)
                .Select(fieldAndChange => new ProjectChangelogEntry(actingUser, project, fieldAndChange.Item1, fieldAndChange.Item2))
            );

            changes.AddRange(ApplyMemberChanges(actingUser, project));
            return changes;
        }

        public List<ProjectUserMembershipChangelogEntry> ApplyMemberChanges(User actingUser, Project project)
        {
            var changes = new List<ProjectUserMembershipChangelogEntry>();

            // Handle member changes or removals
            foreach (ProjectUserMembership membership in project.MemberAssociations) {         
                var newMembership = MemberAssociations.FirstOrDefault(assoc => assoc.User.Id == membership.User.Id);

                if (newMembership == null) { 
                    changes.Add(new(actingUser, project, membership.User, Change<ProjectRole>.Delete(membership.Role)));
                } else if (membership.Role != newMembership.Role) {                   
                    changes.Add(new(actingUser, project, membership.User, Change<ProjectRole>.Update(membership.Role, newMembership.Role)));
                }
            }
            // Handle member additions
            foreach (ProjectUserMembership membership in MemberAssociations) {
                if (project.MemberAssociations.All(assoc => assoc.User.Id != membership.User.Id)) {        
                    changes.Add(new(actingUser, project, membership.User, Change<ProjectRole>.Create(membership.Role)));
                }
            }

            project.MemberAssociations = MemberAssociations.ToList();

            return changes;
        }
    }

    public class GitlabCredentialsForm
    {
        public GitlabCredentialsForm(bool webhooksEnabled) {
            _webhooksEnabled = webhooksEnabled;
        }

        public GitlabCredentialsForm(GitlabCredentials gitlabCredentials, bool webhooksEnabled)
        {
            ProjectId = gitlabCredentials.Id;
            URL = gitlabCredentials.GitlabURL;
            AccessToken = gitlabCredentials.AccessToken;
            PushWebhookSecretToken = gitlabCredentials.PushWebhookSecretToken;  
            _webhooksEnabled = webhooksEnabled;       
        }

        private bool _webhooksEnabled = false;

        /// <summary> Either an error message for how the previous credential check failed or null for no failure </summary>
        [CustomValidation(typeof(GitlabCredentialsForm), nameof(GitlabCredentialsForm.ValidateRequestFailure))]
        public RequestFailure? AuthFailure { get; private set; } = null;

        public bool NeedsChecking { get; private set; } = false;

        private long? _projectId;
        [Required(ErrorMessage = "Project Id is required")]
        public long? ProjectId { 
            get => _projectId; 
            set {
                _projectId = value; 
                GitlabDetailsChanged();
            }
        }

        private string _URLString;
        [CustomValidation(typeof(GitlabCredentialsForm), nameof(GitlabCredentialsForm.ValidateGitlabURL))]
        [Required(ErrorMessage = "Gitlab URL is required")]
        public string URLString { 
            get => _URLString;
            set {
                _URLString = value;

                Uri url = null;
                try
                {
                    url = URL;
                } catch (UriFormatException) {
                }
                if (url != null)
                {
                    _URLString = url.GetLeftPart(UriPartial.Authority);
                }
                
                GitlabDetailsChanged();
            } 
        }
        public Uri URL { 
            get => string.IsNullOrEmpty(URLString) ? null : new UriBuilder(URLString).Uri; 
            set => URLString = value?.ToString(); 
        }

        private string _accessToken;
        [Required(ErrorMessage = "Access Token is required")]
        public string AccessToken { 
            get => _accessToken;
            set {
                _accessToken = value;
                GitlabDetailsChanged();
            }
        }

        private string _pushWebhookSecretToken;        
        [CustomValidation(typeof(GitlabCredentialsForm), nameof(GitlabCredentialsForm.ValidateWebhookSecretToken))]
        public string PushWebhookSecretToken {
            get => _pushWebhookSecretToken;
            set {
                _pushWebhookSecretToken = value;
                GitlabDetailsChanged();
            }
        }
    
        private void GitlabDetailsChanged() {
            NeedsChecking = true;
            AuthFailure = null;
        }

        public static ValidationResult ValidateGitlabURL(string urlString, ValidationContext context) {
            var projectEditForm = context.ObjectInstance as GitlabCredentialsForm;

            try {
                var _ = projectEditForm.URL; // Call getter
            } catch (UriFormatException) {
                return new ValidationResult("Invalid URL format", new[] { context.MemberName });
            }

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateWebhookSecretToken(string secretTokenString, ValidationContext context) {
            var projectEditForm = context.ObjectInstance as GitlabCredentialsForm;

            if (projectEditForm._webhooksEnabled && string.IsNullOrWhiteSpace(secretTokenString))
                return new ValidationResult("Webhook Secret token is required", new[] { context.MemberName });           

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateRequestFailure(RequestFailure? failure, ValidationContext context)
        {
            return failure switch {
                RequestFailure.BadHttpStatus    => new ValidationResult("Invalid GitLab response status", new[] {context.MemberName}),
                RequestFailure.InvalidPayload   => new ValidationResult("Invalid GitLab response format", new[] {context.MemberName}),
                RequestFailure.Forbidden        => new ValidationResult("Insufficient Permissions", new[] {context.MemberName}),
                RequestFailure.Unauthorized     => new ValidationResult("Invalid credentials", new[] {context.MemberName}),
                RequestFailure.NotFound         => new ValidationResult("Project not found", new[] { context.MemberName }),
                RequestFailure.ConnectionFailed => new ValidationResult("Could not connect to server", new[] { nameof(URLString)}),
                _ => ValidationResult.Success,
            };
        }

        public void SetAuthFailure(RequestFailure? authFailure) {
            AuthFailure = authFailure;
            NeedsChecking = false;
        }

        public GitlabCredentials GetCredentials()
        {
            return new GitlabCredentials(URL, ProjectId.Value, AccessToken, PushWebhookSecretToken);
        }
    }
}
