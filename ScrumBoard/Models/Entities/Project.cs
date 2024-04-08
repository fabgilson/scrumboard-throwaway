using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Models.Shapes;
using ScrumBoard.Services;

namespace ScrumBoard.Models.Entities
{
    public class Project : IProjectShape
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        [Display(Name = "Start Date")]
        public DateOnly StartDate { get; set; }
        
        [Display(Name = "End Date")]
        public DateOnly EndDate { get; set; }
        
        public DateTime Created { get; set; }

        private GitlabCredentials _gitlabCredentials;

        [Display(Name = "GitLab Credentials")]
        public GitlabCredentials GitlabCredentials
        {
            // Treat GitlabCredentials with a null ProjectId as the same as null, since it is not possible to properly
            // delete owned entities
            get => _gitlabCredentials?.Id == default(long) ? null : _gitlabCredentials; 
            set => _gitlabCredentials = value;
        }

        [Required]
        [ForeignKey(nameof(CreatorId))]
        public User Creator { get; set; }
        public long CreatorId { get; set; }
        public ICollection<ProjectUserMembership> MemberAssociations { get; set; } = new List<ProjectUserMembership>();
        public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
        
        public ICollection<ProjectFeatureFlag> FeatureFlags { get; set; }

        [Required]
        public Backlog Backlog { get; set; } = new Backlog();

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public bool IsSeedDataProject { get; set; } = false;
    }
}
