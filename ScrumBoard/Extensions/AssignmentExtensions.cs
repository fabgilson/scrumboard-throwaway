using System.Collections.Generic;
using System.Linq;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;

namespace ScrumBoard.Extensions;

public static class AssignmentExtensions
{   
    /// <summary>
    /// Gets the unique recipients for an assignment using its instances.
    /// </summary>
    /// <param name="assignment">The assignment</param>
    /// <param name="projectId">The id of a project to filter for</param>
    /// <returns></returns>
    public static IEnumerable<User> GetUniqueRecipients(this Assignment assignment, long projectId)
    {
        var firstInstance = assignment.Instances.FirstOrDefault();
        if (firstInstance is UserFormInstance)
        {
            return assignment.Instances.Where(a => a.ProjectId == projectId).Select(i => ((UserFormInstance)i).Assignee).Distinct();
        }
        else
        {
            return assignment.Instances.Where(a => a.ProjectId == projectId).SelectMany(i => i.Project.MemberAssociations).Select(a => a.User).Distinct();
        }
    }

    public static IEnumerable<Project> GetProjects(this Assignment assignment)
    {
        return assignment.Instances.Select(x => x.Project).Distinct();
    }
}