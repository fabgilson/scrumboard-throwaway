using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using ScrumBoard.Models.Messages;
using ScrumBoard.Utils;

namespace ScrumBoard.Models.Entities.Changelog
{
    public abstract class ChangelogEntry  : IMessage
    {

        // EF Core needs an empty constructor
        protected ChangelogEntry() {}

        protected ChangelogEntry(User creator, string fieldChanged, Change<object> change, Guid? editingSessionGuid=null) 
            : this(creator.Id, fieldChanged, change, editingSessionGuid) {}

        protected ChangelogEntry(long creatorId, string fieldChanged, Change<object> change, Guid? editingSessionGuid=null) 
        {
            Created = DateTime.Now;
            CreatorId = creatorId;
            Type = change.Type;
            FieldChanged = fieldChanged;
            EditingSessionGuid = editingSessionGuid;
            FromValueObject = change.FromValue;
            ToValueObject = change.ToValue;
        }

        [Key]
        public long Id { get; set; }   

        public long CreatorId { get; set; }
        [ForeignKey(nameof(CreatorId))]
        public User Creator { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public ChangeType Type { get; set; }

        public string FieldChanged { get; set; }

        public string FromValue { get; set; }

        public string ToValue { get; set; }
        
        /// <summary>
        /// A GUID to specify the editing session in which a changelog entry was created. This GUID can be used to track
        /// changes across a single editing session that should only be treated as a single change for the sake of creating
        /// a ChangelogEntry.
        ///
        /// The main use case for this is within editing scenarios that use auto-saving. E.g., if a user in working on a
        /// story review (a component which auto-saves), then any changes they make within a single 'editing session'
        /// should only create a single set of changelogs, regardless of how many times it auto-saves.
        /// 
        /// Note: the scope of an 'editing session' is determined by the components invoking the changes, as they handle
        /// when the GUID is created / updated. Theoretically the scope of an editing session could be the same as the
        /// lifetime of a component, or set to some arbitrary duration (e.g., every 5 minutes the GUID changes).
        /// </summary>
        public Guid? EditingSessionGuid { get; set; }

        /// <summary>
        /// User facing name of the field that is changed
        /// </summary>
        [NotMapped]
        public virtual string FieldChangedName => PropertyChanged?.GetCustomAttribute<DisplayAttribute>()?.Name ?? FieldChanged;

        [NotMapped] 
        protected virtual PropertyInfo PropertyChanged => EntityType.GetProperty(FieldChanged);

        [NotMapped]
        public virtual Type FieldType => PropertyChanged.PropertyType;

        [NotMapped]
        private TypeConverter Converter => TypeDescriptor.GetConverter(FieldType);

        [NotMapped]
        public virtual object FromValueObject {
            get {
                if (FromValue == null) return null;
                return Converter.ConvertFromString(FromValue);
            }
            set {
                if (value == null) {
                    FromValue = null;
                } else {
                    FromValue = Converter.ConvertToString(value);
                }
            }
        }

        [NotMapped]
        public virtual object ToValueObject {
            get {
                if (ToValue == null) return null;
                return Converter.ConvertFrom(ToValue);
            }
            set {
                if (value == null) {
                    ToValue = null;
                } else {
                    ToValue = Converter.ConvertToString(value);
                }
            }
        }

        [NotMapped]
        public abstract Type EntityType { get; }

        /// <summary>
        /// Defines a generate message function. 
        /// To be implemented by specific entity changelog subclasses. 
        /// </summary>
        /// <return> A message in the format: 
        /// "{this.Created} - {this.Creator.FirstName} {this.Creator.LastName} {this.Type} an {entity}";
        /// </return>
        public abstract List<IMessageToken> GenerateMessage();
    }
}
