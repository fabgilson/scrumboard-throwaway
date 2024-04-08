using ScrumBoard.Models.Entities;
using ScrumBoard.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ScrumBoard.Models.Forms
{
    public class SelectUsersForm
    {
        [CustomValidation(typeof(SelectUsersForm), "ValidateLeaderPresent")]
        public List<ProjectUserMembership> Associations { get; set; } = null;

        public static ValidationResult ValidateLeaderPresent(List<ProjectUserMembership> Associations, ValidationContext context) {
            var selectUsersForm = context.ObjectInstance as SelectUsersForm;

            if (Associations != null) {
                bool atLeastOneLeader = false;
                foreach (ProjectUserMembership association in Associations) {
                    if (association.Role == ProjectRole.Leader) {
                        atLeastOneLeader = true;
                    }
                }    
                if (!atLeastOneLeader) {
                    return new ValidationResult("A project must have at least one leader");
                }            
            }
            return ValidationResult.Success;
        }

    }
}