using System;
using ScrumBoard.Models.Entities;
using System.Collections.Generic;
using System.Linq;

namespace ScrumBoard.Extensions
{
    public static class UserExtensions
    {
        /// <summary>
        /// Gets all projects associated with this user
        /// </summary>
        /// <returns>A list of projects</returns>
        public static List<Project> GetProjects(this User user)
        { 
            return user.ProjectAssociations.Select(assoc => assoc.Project).ToList(); 
        }

        /// <summary>
        /// Determines whether the given user has permission to view this project
        /// </summary>
        /// <returns>Boolean indicating whether the user has permission to see the project</returns>
        public static bool CanView(this User user, Project project)
        { 
            return user.GetProjects().Any(userProject => userProject.Id == project.Id) 
                   || project.MemberAssociations.Any(assoc => assoc.UserId == user.Id);
        }
        
        /// <summary>
        /// Retrieves the current role of the given user in a project
        /// </summary>
        /// <returns>The user's role in a given project or null if they do not have a role within the project</returns>
        public static ProjectRole? GetProjectRole(this User user, Project project)
        {
            return project is null ? null : user.GetProjectRole(project.Id);
        }

        /// <summary>
        /// Retrieves the current role of the given user in a project
        /// </summary>
        /// <returns>The user's role in a given project or null if they do not have a role within the project</returns>
        public static ProjectRole? GetProjectRole(this User user, long projectId)
        {
            return user.ProjectAssociations.SingleOrDefault(assoc => assoc.ProjectId == projectId)?.Role;
        }

        /// <summary>
        /// Retrieves all of the given user's assigned user story tasks
        /// </summary>
        /// <returns>All of the user's UserStoryTask instances in their UserAssociations attribute</returns>
        public static List<UserStoryTask> GetAssignedTasks(this User user)
        {
            return user.TaskAssociations.Where(a => a.Role.Equals(TaskRole.Assigned)).Select(a => a.Task).ToList();
        }

        /// <summary>
        /// Retrieves all of the given user's user story tasks to review
        /// </summary>
        /// <returns>All of the user's UserStoryTask instances in their ReviewingAsosciations attribute</returns>
        public static List<UserStoryTask> GetReviewingTasks(this User user)
        {
            return user.TaskAssociations.Where(a => a.Role.Equals(TaskRole.Reviewer)).Select(a => a.Task).ToList();
        }

        /// <summary>
        /// Assigns a given task to a user. 
        /// Creates a new association and adds it to both entitie's collections. 
        /// </summary>
        /// <returns>A UserTaskAssociation association between the user and task</returns>
        public static UserTaskAssociation AssignTask(this User user, UserStoryTask task)
        {
            UserTaskAssociation association = new UserTaskAssociation() 
            {
                UserId = user.Id,  
                User = user,              
                TaskId = task.Id,              
                Task = task,
                Role = TaskRole.Assigned
            };
            user.TaskAssociations.Add(association);
            task.UserAssociations.Add(association);
            return association;
        }

        /// <summary>
        /// Removes the assignment association to the given task from the user
        /// </summary>
        public static void RemoveTaskAssignment(this User user, UserStoryTask task) {
            UserTaskAssociation association = user.TaskAssociations
                .Where(assoc => assoc.TaskId.Equals(task.Id) && assoc.UserId.Equals(user.Id) && assoc.Role.Equals(TaskRole.Assigned))
                .FirstOrDefault();
            user.TaskAssociations.Remove(association);
            task.UserAssociations.Remove(association);
        }

        /// <summary>
        /// Removes the reviewing association to the given task from the user
        /// </summary>
        public static void RemoveTaskReview(this User user, UserStoryTask task) {
            UserTaskAssociation association = user.TaskAssociations
                .Where(assoc => assoc.TaskId.Equals(task.Id) && assoc.UserId.Equals(user.Id) && assoc.Role.Equals(TaskRole.Reviewer)).FirstOrDefault();
            user.TaskAssociations.Remove(association);
            task.UserAssociations.Remove(association);
        }

        /// <summary>
        /// Assigns a given task to a user to review. 
        /// Creates a new association and adds it to both entities's collections. 
        /// </summary>
        /// <returns>A UserTaskAssociation between the user and task</returns>
        public static UserTaskAssociation ReviewTask(this User user, UserStoryTask task)
        {
            UserTaskAssociation association = new UserTaskAssociation() 
            {
                UserId = user.Id,     
                User = user,         
                TaskId = task.Id,            
                Task = task,
                Role = TaskRole.Reviewer
            };
            user.TaskAssociations.Add(association);
            task.UserAssociations.Add(association);
            return association;
        }

        /// <summary>
        /// Formats the user's name attributes in a readable manner
        /// </summary>
        /// <returns>A string of the user's first and last name attributes with a separating space character between</returns>
        public static string GetFullName(this User user) {
            return user.FirstName + " " + user.LastName;
        }
    }
}