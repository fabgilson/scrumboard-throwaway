using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Validators;

namespace ScrumBoard.Models.Forms.Feedback;

public class LinkedProjects
{
    public Project FirstProject { get; set; }
    public Project SecondProject { get; set; }
}

public class FormTemplateAssignmentForm
{
    public FormTemplateAssignmentForm()
    {
    }

    public FormTemplateAssignmentForm(FormTemplate formTemplate)
    {
        FormTemplate = formTemplate;
    }

    public FormTemplate FormTemplate { get; set; }

    [CustomValidation(typeof(FormTemplateAssignmentForm), nameof(ValidateSelectedProjects))]
    public IEnumerable<Project> SelectedSingleProjects { get; set; } = new List<Project>();
    
    [CustomValidation(typeof(FormTemplateAssignmentForm), nameof(ValidateLinkedSelectedProjects))]
    public IEnumerable<LinkedProjects> SelectedLinkedProjects { get; set; } = new List<LinkedProjects> { new LinkedProjects() };

    [Required(AllowEmptyStrings = false, ErrorMessage = "Name cannot be empty")]
    [MaxLength(50, ErrorMessage = "Name cannot be longer than 50 characters")]
    [NotEntirelyNumbersOrSpecialCharacters(ErrorMessage = "Name cannot only contain numbers or special characters")]
    public string Name { get; set; }

    [CustomValidation(typeof(FormTemplateAssignmentForm), nameof(ValidateStartDate))]
    public DateTime? StartDate { get; set; }

    [Required] 
    public DateTime? EndDate { get; set; }

    [CustomValidation(typeof(FormTemplateAssignmentForm), nameof(ValidateSelectedRoles))]
    public IDictionary<ProjectRole, bool> SelectedRoles { get; set; } =
        Enum.GetValues<ProjectRole>().ToDictionary(x => x, y => y == ProjectRole.Developer);

    [Required] 
    public AssignmentType AssignmentType { get; set; }

    public bool StartDateEnabled { get; set; }
    
    [Required]
    public bool AllowSavingBeforeStartDate { get; set; }

    public static ValidationResult ValidateStartDate(DateTime? startDate,
        ValidationContext context)
    {
        var form = (FormTemplateAssignmentForm)context.ObjectInstance;

        if (form.StartDateEnabled)
            return startDate < form.EndDate
                ? ValidationResult.Success
                : new ValidationResult("The start date must be before the end date", new[] { nameof(form.StartDate) });

        return ValidationResult.Success;
    }

    public static ValidationResult ValidateSelectedRoles(IDictionary<ProjectRole, bool> selectedRoles,
        ValidationContext context)
    {
        var form = (FormTemplateAssignmentForm)context.ObjectInstance;
        if (form.AssignmentType is AssignmentType.Team) return ValidationResult.Success;
        
        if (selectedRoles.Any(x => x.Value)) return ValidationResult.Success;

        return new ValidationResult("At least one role must be selected", new[] { context.MemberName });
    }

    public static ValidationResult ValidateSelectedProjects(IEnumerable<Project> selectedProjects,
        ValidationContext context)
    {
        var form = (FormTemplateAssignmentForm)context.ObjectInstance;

        if (form.AssignmentType is AssignmentType.Team) return ValidationResult.Success;
        
        if (selectedProjects.Any()) return ValidationResult.Success;

        return new ValidationResult("At least one project must be selected", new[] { context.MemberName });
    }
    
    public static ValidationResult ValidateLinkedSelectedProjects(IEnumerable<LinkedProjects> selectedLinkedProjects,
        ValidationContext context)
    {
        var form = (FormTemplateAssignmentForm)context.ObjectInstance;
        
        if (form.AssignmentType is not AssignmentType.Team) return ValidationResult.Success;
        
        var linkedProjects = selectedLinkedProjects.ToList();

        if (linkedProjects.Any(x => x.FirstProject is null && x.SecondProject is null))
        {
            return new ValidationResult("Selected projects cannot be empty", new[] { context.MemberName });
        }
   
        if (linkedProjects.Any(x => x.FirstProject is null || x.SecondProject is null))
        {
            return new ValidationResult("Project pairs cannot have only one project", new[] { context.MemberName });
        }
        
        if (linkedProjects.Count == 0)
        {
            return new ValidationResult("At least one pair of projects must be added", new[] { context.MemberName });
        }
        
        return ValidationResult.Success;
    }
}