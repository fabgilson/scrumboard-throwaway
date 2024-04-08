using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Models.Gitlab
{
    public class GitlabCommit
    {
        /// <summary>Commit SHA</summary>
        [Key]
        [Column(TypeName = "varchar(95)")]
        public string Id { get; set; }
        
        /// <summary>
        /// The browser accessbile URL of the Gitlab commit. 
        /// E.g. 'https://gitlab.example.com/project/gitlab-foss/-/commit/ed899a2f4b50b4370feeea94676502b42383c746'
        /// </summary>
        [Required]
        public Uri WebUrl { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        [Required]
        public string AuthorName { get; set; }

        [Required]
        public string AuthorEmail { get; set; }
        
        [Required]
        public DateTime AuthoredDate { get; set; }

        public ICollection<WorklogEntry> RelatedWorklogEntries { get; set; } = new List<WorklogEntry>();
    }
}