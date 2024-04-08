using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Gitlab;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{
    public class WorklogEntryCommitChangelogEntry : WorklogEntryChangelogEntry
    {
        public override Type FieldType => throw new NotSupportedException();
        
        public override string FieldChangedName => throw new NotSupportedException();

        [Column(TypeName = "varchar(95)")]
        public string CommitChangedId { get; set; }
        
        [ForeignKey(nameof(CommitChangedId))]
        public GitlabCommit CommitChanged { get; set; }

        public static WorklogEntryCommitChangelogEntry Add(long creatorId, long worklogEntryChangedId, GitlabCommit commitAdded)
        {
            return new WorklogEntryCommitChangelogEntry(creatorId, worklogEntryChangedId, commitAdded, Change<object>.Create(null));
        }
        
        public static WorklogEntryCommitChangelogEntry Remove(long creatorId, long worklogEntryChangedId, GitlabCommit commitRemoved)
        {
            return new WorklogEntryCommitChangelogEntry(creatorId, worklogEntryChangedId, commitRemoved, Change<object>.Delete(null));
        }

        // EF Core needs an empty constructor
        public WorklogEntryCommitChangelogEntry(){}

        private WorklogEntryCommitChangelogEntry(long creatorId, long worklogEntryChangedId, GitlabCommit commitChanged, Change<object> change) 
            : base(creatorId, worklogEntryChangedId, null, change)
        {
            CommitChangedId = commitChanged.Id;
        }

        private List<IMessageToken> GenerateCreateMessage() {
            return new List<IMessageToken>
            {
                new TextToken($"{Creator.GetFullName()} added commit"),
                new ValueToken(CommitChanged.Title),
            };
        }

        private List<IMessageToken> GenerateDeleteMessage() {       
            return new List<IMessageToken>
            {
                new TextToken($"{Creator.GetFullName()} removed commit"),
                new ValueToken(CommitChanged.Title),
            };
        }

        public override List<IMessageToken> GenerateMessage() {
            switch (Type)
            {
                case ChangeType.Create:
                    return GenerateCreateMessage();
                case ChangeType.Delete:
                    return GenerateDeleteMessage();  
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
