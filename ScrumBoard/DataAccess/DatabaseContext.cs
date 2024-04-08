using System.Reflection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Announcements;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Entities.FeatureFlags;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Models.Gitlab;

namespace ScrumBoard.DataAccess
{
    public class DatabaseContext : DbContext, IDataProtectionKeyContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectUserMembership> ProjectUserMemberships { get; set; } = null!;
        public DbSet<Backlog> Backlogs { get; set; } = null!;
        public DbSet<Sprint> Sprints { get; set; } = null!;
        public DbSet<UserStory> UserStories { get; set; } = null!;
        public DbSet<AcceptanceCriteria> AcceptanceCriterias { get; set; } = null!;
        public DbSet<ChangelogEntry> ChangelogEntries { get; set; } = null!;
        // EF does not automatically scan for derived types. Therefore DBSets for subclasses must be explicitly specified.
        // However, EF does map this inheritance using table-per-hierarchy by default. I.e. They will all be in one table.
        public DbSet<ProjectChangelogEntry> ProjectChangelogEntries { get; set; } = null!;
        public DbSet<ProjectUserMembershipChangelogEntry> ProjectUserMembershipChangelogEntries { get; set; } = null!;
        public DbSet<UserStoryChangelogEntry> UserStoryChangelogEntries { get; set; } = null!;
        public DbSet<AcceptanceCriteriaChangelogEntry> AcceptanceCriteriaChangelogEntries { get; set; } = null!;
        public DbSet<UserTaskAssociation> UserTaskAssociations { get; set; } = null!;
        public DbSet<UserTaskAssociationChangelogEntry> UserTaskAssociationChangelogEntries { get; set; } = null!;
        public DbSet<UserStoryTaskChangelogEntry> UserStoryTaskChangelogEntries { get; set; } = null!;
        public DbSet<UserStoryTaskTagChangelogEntry> UserStoryTaskTagChangelogEntries { get; set; } = null!;
        public DbSet<SprintChangelogEntry> SprintChangelogEntries { get; set; } = null!;
        public DbSet<SprintStoryAssociationChangelogEntry> SprintStoryAssociationChangelogEntries { get; set; } = null!;        
        public DbSet<WorklogEntryChangelogEntry> WorklogEntryChangelogEntries { get; set; } = null!;
        public DbSet<WorklogEntryUserAssociationChangelogEntry> WorklogEntryUserAssociationChangelogEntries { get; set; } = null!;
        public DbSet<TaggedWorkInstanceChangelogEntry> WorklogEntryTagChangelogEntries { get; set; } = null!;
        public DbSet<WorklogEntryCommitChangelogEntry> WorklogEntryCommitChangelogEntries { get; set; } = null!;
        public DbSet<OverheadEntryChangelogEntry> OverheadEntryChangelogEntries { get; set; } = null!;
        public DbSet<OverheadEntrySessionChangelogEntry> OverheadEntrySessionChangelogEntries { get; set; } = null!;
        public DbSet<StandUpMeetingChangelogEntry> StandUpMeetingChangelogEntries { get; set; } = null!;
        public DbSet<StandUpMeetingUserMembershipChangelogEntry> StandUpMeetingUserMembershipChangelogEntries { get; set; } = null!;
        public DbSet<TaggedWorkInstanceChangelogEntry> TaggedWorkInstanceChangelogEntries { get; set; } = null!;
        public DbSet<WeeklyReflectionCheckInChangelogEntry> WeeklyReflectionCheckInChangelogEntries { get; set; } = null!;
        public DbSet<UserStoryTask> UserStoryTasks { get; set; } = null!;
        public DbSet<UserStoryTaskTag> UserStoryTaskTags { get; set; } = null!;
        public DbSet<UserStoryTaskTagJoin> UserStoryTaskTagJoins { get; set; } = null!;
        public DbSet<WorklogEntry> WorklogEntries { get; set; } = null!;
        public DbSet<WorklogTag> WorklogTags { get; set; } = null!;
        public DbSet<TaggedWorkInstance> TaggedWorkInstances { get; set; } = null!;
        
        // Form Templates
        public DbSet<FormTemplateBlock> FormTemplateBlocks { get; set; } = null!;
        public DbSet<PageBreak> PageBreaks { get; set; } = null!;
        public DbSet<TextBlock> TextBlocks { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<MultiChoiceQuestion> MultichoiceQuestions { get; set; } = null!;
        public DbSet<MultiChoiceOption> MultiChoiceOption { get; set; } = null!;
        public DbSet<TextQuestion> TextQuestions { get; set; } = null!;
        public DbSet<FormTemplate> FormTemplates { get; set; } = null!;
        
        // Form Instances
        public DbSet<FormInstance> FormInstances { get; set; } = null!;
        public DbSet<UserFormInstance> UserFormInstances { get; set; } = null!;
        public DbSet<TeamFormInstance> TeamFormInstances { get; set; } = null!;
        public DbSet<Answer> Answers { get; set; } = null!;
        public DbSet<TextAnswer> TextAnswers { get; set; } = null!;
        public DbSet<MultiChoiceAnswer> MultichoiceAnswers { get; set; } = null!;
        public DbSet<Assignment> Assignments { get; set; } = null!;
        public DbSet<MultichoiceAnswerMultichoiceOption> MultichoiceAnswerMultichoiceOption { get; set; } = null!;

        public DbSet<OverheadEntry> OverheadEntries { get; set; } = null!;
        public DbSet<OverheadSession> OverheadSessions { get; set; } = null!;
        public DbSet<GitlabCommit> GitlabCommits { get; set; } = null!;
        public DbSet<WorklogCommitJoin> WorklogCommitJoins { get; set; } = null!;
        public DbSet<Announcement> Announcements { get; set; } = null!;
        public DbSet<AnnouncementHide> AnnouncementHides { get; set; } = null!;
        public DbSet<ProjectFeatureFlag> ProjectFeatureFlags { get; set; } = null!;

        public DbSet<StandUpMeeting> StandUpMeetings { get; set; } = null!;
        public DbSet<StandUpMeetingAttendance> StandUpMeetingAttendance { get; set; } = null!;
        public DbSet<WeeklyReflectionCheckIn> WeeklyReflectionCheckIns { get; set; } = null!;
        public DbSet<TaskCheckIn> TaskCheckIns { get; set; } = null!;
        public DbSet<UserStandUpCalendarLink> UserStandUpCalendarLinks { get; set; } = null!;

        public DbSet<SinglePerUserFlag> SinglePerUserFlags { get; set; } = null!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
