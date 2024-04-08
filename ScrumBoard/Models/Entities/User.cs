using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ScrumBoard.Models.Entities.Relationships;

namespace ScrumBoard.Models.Entities
{
    public class User
    {
        /// <summary>
        /// The User ID of this entity must match the ID of an existing User in the IdentityProvider
        /// </summary>
        [Key]
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string LDAPUsername { get; set; }

        public string Email { get; set; }
        public ICollection<ProjectUserMembership> ProjectAssociations { get; set; } = new List<ProjectUserMembership>();       

        public ICollection<UserTaskAssociation> TaskAssociations { get; set; } = new List<UserTaskAssociation>();
        
        public ICollection<StandUpMeetingAttendance> StandUpMeetingAttendances { get; set; } = new List<StandUpMeetingAttendance>();

        public override string ToString()
        {
            return $"User(FirstName={FirstName}, LastName={LastName}, Id={Id})";
        }
    }
}
