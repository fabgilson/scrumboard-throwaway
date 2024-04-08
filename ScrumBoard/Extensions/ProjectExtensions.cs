using ScrumBoard.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ScrumBoard.Extensions
{
    public static class ProjectExtensions
    {
        // The roles within a team that are considered 'working members', for displaying stats and sidebar profile icons
        private static readonly IEnumerable<ProjectRole> _workingMemberRoles = new[] { ProjectRole.Developer };

        /// <summary>
        /// Gets all users associated with this project
        /// </summary>
        /// <returns>An enumerable of member users</returns>
        public static IEnumerable<User> GetMembers(this Project project)
        { 
            return project.MemberAssociations.Select(assoc => assoc.User); 
        }

        /// <summary>
        /// Retrieves a working member from the project based on the provided member ID.
        /// </summary>
        /// <param name="project">The project from which to retrieve the working member.</param>
        /// <param name="memberId">The ID of the working member to retrieve.</param>
        /// <returns>The working member with the specified ID if found; otherwise, null.</returns>
        public static User GetWorkingMemberById(this Project project, long memberId)
        {
            return project.GetWorkingMembers().FirstOrDefault(member => member.Id == memberId);
        }
        
                /// <summary>
        /// Gets all users that can work on this project
        /// </summary>
        /// <returns>An enumerable of member users</returns>
        public static IEnumerable<User> GetWorkingMembers(this Project project)
        { 
            return project.MemberAssociations
                .Where(membership => _workingMemberRoles.Contains(membership.Role))
                .Select(assoc => assoc.User); 
        }

        /// <summary>
        /// Retrieves the current role of the given user in a project
        /// </summary>
        /// <returns>The user's role in a given project or null if no membership is found</returns>
        public static ProjectRole? GetRole(this Project project, User user)
        {
            return project.MemberAssociations
                .Where(assoc => assoc.UserId == user.Id)
                .Select(membership => membership.Role as ProjectRole?)
                .SingleOrDefault();
        }

        /// <summary>
        /// Gets the sprint that is either in progress or about to start
        /// </summary>
        /// <returns>Current sprint or null if no sprints are current</returns>
        public static Sprint GetCurrentSprint(this Project project)
        {
            return project.Sprints.FirstOrDefault(sprint => sprint.Stage is SprintStage.Created or SprintStage.Started);
        }
        
        /// <summary>
        /// Gets that sprint that can now be reviewed
        /// </summary>
        /// <summary>Reviewable sprint or null if no sprints can be reviewed </summary>
        public static Sprint GetReviewableSprint(this Project project) 
        {
            return project.Sprints.FirstOrDefault(sprint => sprint.Stage == SprintStage.ReadyToReview);
        }

        /// <summary>
        /// Gets that sprint that is under review
        /// </summary>
        /// <summary>Reviewing sprint or null if no sprints can is under review </summary>
        public static Sprint GetReviewingSprint(this Project project) 
        {
            return project.Sprints.FirstOrDefault(sprint => sprint.Stage == SprintStage.InReview);
        }

        /// <summary>
        /// Gets all the stories for this project that are in the backlog
        /// i.e. Not part of any sprint
        /// </summary>
        /// <returns> Stories in the project backlog </summary>
        public static IEnumerable<UserStory> GetBacklogStories(this Project project) {
            return project.Backlog.Stories;
        }

        /// <summary>
        /// Gets the end date of the last sprint that finished. Returns null if there have been no previous finished sprints.
        /// </summary>
        /// <returns> The end date of the last sprint that finished </summary>
        public static DateOnly? GetLastSprintEndDate(this Project project) {
            Sprint sprint = project.Sprints.Where(sprint => 
                sprint.Stage.IsWorkDone()
            ).OrderByDescending(sprint => sprint.EndDate).FirstOrDefault();
            return sprint == null ? null : sprint.EndDate;       
        }
    }
}