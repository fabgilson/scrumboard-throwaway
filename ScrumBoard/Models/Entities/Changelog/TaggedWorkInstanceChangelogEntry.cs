using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{
    public class TaggedWorkInstanceChangelogEntry : WorklogEntryChangelogEntry
    {
        public override Type FieldType => typeof(TaggedWorkInstance);

        public long? TaggedWorkInstanceId { get; set; }

        [ForeignKey(nameof(TaggedWorkInstanceId))]
        public TaggedWorkInstance TaggedWorkInstance { get; set; }

        public long WorklogTagId { get; set; }
        [ForeignKey(nameof(WorklogTagId))] public WorklogTag WorklogTag { get; set; }

        public long WorklogEntryId { get; set; }
        [ForeignKey(nameof(WorklogEntryId))] public WorklogEntry WorklogEntry { get; set; }

        private TaggedWorkInstance GetToValueObjectWithWorklogTag()
        {
            var toValueObject = (TaggedWorkInstance)ToValueObject;
            toValueObject.WorklogTag = WorklogTag;
            return toValueObject;
        }
        
        private TaggedWorkInstance GetFromValueObjectWithWorklogTag()
        {
            var fromValueObject = (TaggedWorkInstance)FromValueObject;
            fromValueObject.WorklogTag = WorklogTag;
            return fromValueObject;
        }

        public static TaggedWorkInstanceChangelogEntry Add(long creatorId, long worklogEntryId, long worklogTagId, TaggedWorkInstance taggedWorkInstance)
        {
            return new TaggedWorkInstanceChangelogEntry(creatorId, worklogEntryId, worklogTagId, taggedWorkInstance.Id, null, Change<object>.Create(taggedWorkInstance));
        }
        
        public static TaggedWorkInstanceChangelogEntry Remove(long creatorId, long worklogEntryId, long worklogTagId, TaggedWorkInstance taggedWorkInstance)
        {
            return new TaggedWorkInstanceChangelogEntry(creatorId, worklogEntryId, worklogTagId, null, null, Change<object>.Delete(taggedWorkInstance));
        }
        
        public static TaggedWorkInstanceChangelogEntry Update(long creatorId, long worklogEntryId, long worklogTagId, TaggedWorkInstance incoming, TaggedWorkInstance existing)
        {
            return new TaggedWorkInstanceChangelogEntry(creatorId, worklogEntryId, worklogTagId, incoming.Id, null, Change<object>.Update(existing, incoming));
        }

        // EF Core needs an empty constructor
        public TaggedWorkInstanceChangelogEntry(){}

        public TaggedWorkInstanceChangelogEntry(long creatorId, long worklogEntryId, long worklogTagId, long? taggedWorkInstanceId, string fieldChangedName, Change<object> change)
            : base(creatorId, worklogEntryId, fieldChangedName, change)
        {
            TaggedWorkInstanceId = taggedWorkInstanceId;
            WorklogEntryId = worklogEntryId;
            WorklogTagId = worklogTagId;
        }

        private List<IMessageToken> GenerateCreateMessage() {
            return new List<IMessageToken>
            {
                new TextToken($"{Creator.GetFullName()} added work instance"), 
                new ValueToken(GetToValueObjectWithWorklogTag())
            };
        }

        private List<IMessageToken> GenerateDeleteMessage() {       
            return new List<IMessageToken>
            {
                new TextToken($"{Creator.GetFullName()} removed a work instance"), 
                new ValueToken(GetFromValueObjectWithWorklogTag())
            };
        }
        
        private List<IMessageToken> GenerateUpdateMessage() {       
            return new List<IMessageToken>
            {
                new TextToken($"{Creator.GetFullName()} updated work instance"),
                new DifferenceToken(GetFromValueObjectWithWorklogTag(), GetToValueObjectWithWorklogTag()),
            };
        }

        public override List<IMessageToken> GenerateMessage()
        {
            return Type switch
            {
                ChangeType.Create => GenerateCreateMessage(),
                ChangeType.Delete => GenerateDeleteMessage(),
                ChangeType.Update => GenerateUpdateMessage(),
                _ => throw new NotSupportedException()
            };
        }
    }
}
