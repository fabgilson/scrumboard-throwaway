using System;
using ScrumBoard.Models.Entities.Forms.Instances;

namespace ScrumBoard.Extensions;

public static class FormInstanceExtensions
{
    public static string GetAssignmentDescription(this FormInstance formInstance)
    {
        return formInstance switch
        {
            TeamFormInstance teamFormInstance 
                => $"{teamFormInstance.Project.Name}'s team form for {teamFormInstance.LinkedProject.Name}",
            
            UserFormInstance { Pair: not null } pairedFormInstance 
                => $"{pairedFormInstance.Assignee.GetFullName()}'s pairwise form for {pairedFormInstance.Pair.GetFullName()}",
            
            UserFormInstance individualFormInstance 
                => $"{individualFormInstance.Assignee.GetFullName()}'s individual form",
            
            _ => throw new InvalidOperationException($"Unexpected instance of FormInstance received: {formInstance.GetType()}")
        };
    }
}