using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Gitlab;

namespace ScrumBoard.Models.Entities.Relationships
{
    public class WorklogCommitJoin
    {
        public WorklogCommitJoin() { }
        public WorklogCommitJoin(WorklogEntry entry, GitlabCommit commit)
        {
            EntryId = entry.Id;
            CommitId = commit.Id;
        }

        public long EntryId { get; set; }
        public WorklogEntry Entry { get; set; }
        
        [Column(TypeName = "varchar(95)")]
        public string CommitId { get; set; }
        public GitlabCommit Commit { get; set; }
    }
}