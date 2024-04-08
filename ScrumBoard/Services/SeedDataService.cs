using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Extensions;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Utils;

namespace ScrumBoard.Services
{
    public interface ISeedDataService
    {
        Task SeedInitialDataAsync();

        Task AddUserToGeneratedProjects(User user);

        Task CreateTagsAndSessions();
    }

    public class SeedDataService : ISeedDataService
    {
        private readonly IDbContextFactory<DatabaseContext> contextFactory;

        private UserStoryTaskTag spikeTaskTag         = new() { Name = "Research Spike", Style = BadgeStyle.Light };
        private UserStoryTaskTag wireframingTag       = new() { Name = "Wireframing",    Style = BadgeStyle.Light };
        private UserStoryTaskTag stylingTag           = new() { Name = "Styling",        Style = BadgeStyle.Light };
        private UserStoryTaskTag refactoringTaskTag   = new() { Name = "Refactoring",    Style = BadgeStyle.Light };
        private UserStoryTaskTag reengineeringTaskTag = new()  {Name = "Reengineering",  Style = BadgeStyle.Light };
        
        // If you are changing any of these tag names, check the StatisticsFilters class to ensure name-based filtering
        // still works
        private WorklogTag featureTag  = new () { Name = "Feature",  Style=BadgeStyle.Light };
        private WorklogTag fixTag      = new () { Name = "Fix",      Style=BadgeStyle.Light };
        private WorklogTag testTag     = new () { Name = "Test",     Style=BadgeStyle.Light };
        private WorklogTag documentTag = new () { Name = "Document", Style=BadgeStyle.Light };
        private WorklogTag choreTag    = new () { Name = "Chore",    Style=BadgeStyle.Light };
        private WorklogTag spikeTag    = new () { Name = "Spike",    Style=BadgeStyle.Light };
        private WorklogTag refactorTag = new () { Name = "Refactor", Style=BadgeStyle.Light };
        private WorklogTag reviewTag = new () { Name = "Review", Style=BadgeStyle.Light };
        private WorklogTag manualTestTag = new () { Name = "Testmanual", Style=BadgeStyle.Light };
        private WorklogTag reengineeringTag = new () { Name = "Reengineer", Style=BadgeStyle.Light };

        private OverheadSession planning1Session     = new() { Name = "Planning 1"    };
        private OverheadSession planning2Session     = new() { Name = "Planning 2"    };
        private OverheadSession standupSession       = new() { Name = "Daily Scrum"       };
        private OverheadSession retrospectiveSession = new() { Name = "Retrospective" };
        private OverheadSession sprintReviewSession  = new() { Name = "Sprint Review" };
        private OverheadSession workshopSession = new() { Name = "Workshop" };
        private OverheadSession reflectionSession = new() { Name = "Reflection" };
        private OverheadSession demoSession = new() { Name = "Demo" };
        private OverheadSession backlogGroomingSession = new() { Name = "Backlog Grooming" };

        public SeedDataService(IDbContextFactory<DatabaseContext> dbContextFactory)
        {
            contextFactory = dbContextFactory;
        }

        public async Task AddUserToGeneratedProjects(User user)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var projects = context.Projects.Where(p => p.IsSeedDataProject == true);
            foreach (var project in projects)
            {                
                // Review project has Id set to 200, so make sure we're added as a reviewer
                var role = project.Id == 200 ? ProjectRole.Reviewer : ProjectRole.Leader;
                project.MemberAssociations.Add(new ProjectUserMembership() { 
                    UserId = user.Id, 
                    ProjectId = project.Id, 
                    Role = role,
                });
            }
            await context.SaveChangesAsync();
        }

        private async Task<UserStoryTaskTag> CreateOrLoadTag(UserStoryTaskTag defaultTag)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var foundTag = await context.UserStoryTaskTags
                .Where(tag => tag.Name == defaultTag.Name)
                .SingleOrDefaultAsync();
            if (foundTag != null) return foundTag;

            context.UserStoryTaskTags.Add(defaultTag);
            await context.SaveChangesAsync();
            return defaultTag;
        }
        
        private async Task<WorklogTag> CreateOrLoadTag(WorklogTag defaultTag)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var foundTag = await context.WorklogTags
                .Where(tag => tag.Name == defaultTag.Name)
                .SingleOrDefaultAsync();
            if (foundTag != null) return foundTag;

            context.WorklogTags.Add(defaultTag);
            await context.SaveChangesAsync();
            return defaultTag;
        }

        private async Task<OverheadSession> CreateOrLoadSession(OverheadSession defaultSession)
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var foundSession = await context.OverheadSessions
                .Where(tag => tag.Name == defaultSession.Name)
                .SingleOrDefaultAsync();
            if (foundSession != null) return foundSession;

            context.OverheadSessions.Add(defaultSession);
            await context.SaveChangesAsync();
            return defaultSession;
        }
        
        public async Task CreateTagsAndSessions()
        {
            var taskGenerators = new List<Func<Task>>
            {
                async () => spikeTaskTag = await CreateOrLoadTag(spikeTaskTag),
                async () => wireframingTag = await CreateOrLoadTag(wireframingTag),
                async () => stylingTag = await CreateOrLoadTag(stylingTag),
                async () => refactoringTaskTag = await CreateOrLoadTag(refactoringTaskTag),
                async () => reengineeringTaskTag = await CreateOrLoadTag(reengineeringTaskTag),

                async () => featureTag = await CreateOrLoadTag(featureTag),
                async () => fixTag = await CreateOrLoadTag(fixTag),
                async () => testTag = await CreateOrLoadTag(testTag),
                async () => documentTag = await CreateOrLoadTag(documentTag),
                async () => choreTag = await CreateOrLoadTag(choreTag),
                async () => spikeTag = await CreateOrLoadTag(spikeTag),
                async () => refactorTag = await CreateOrLoadTag(refactorTag),
                async () => reviewTag = await CreateOrLoadTag(reviewTag),
                async () => manualTestTag = await CreateOrLoadTag(manualTestTag),
                async () => reengineeringTag = await CreateOrLoadTag(reengineeringTag),
                
                async () => planning1Session = await CreateOrLoadSession(planning1Session),
                async () => planning2Session = await CreateOrLoadSession(planning2Session),
                async () => standupSession = await CreateOrLoadSession(standupSession),
                async () => retrospectiveSession = await CreateOrLoadSession(retrospectiveSession),
                async () => sprintReviewSession = await CreateOrLoadSession(sprintReviewSession),
                async () => workshopSession = await CreateOrLoadSession(workshopSession),
                async () => reflectionSession = await CreateOrLoadSession(reflectionSession),
                async () => demoSession = await CreateOrLoadSession(demoSession),
                async () => backlogGroomingSession = await CreateOrLoadSession(backlogGroomingSession),
            };
            await Task.WhenAll(taskGenerators.Select(generator => generator()));
        }

        public async Task SeedInitialDataAsync()
        {
            await CreateTagsAndSessions();
            await using var context = await contextFactory.CreateDbContextAsync();
            
            // Ensure that ef core does not try to update any tags when seeding tasks/worklog entries
            foreach (var tag in new ITag[] { 
                // Task tags
                spikeTaskTag, wireframingTag, stylingTag, refactoringTaskTag, reengineeringTaskTag,  
                
                // Worklog tags
                featureTag, fixTag, testTag, documentTag, choreTag, spikeTag, 
                refactorTag, reviewTag, manualTestTag, reengineeringTag
            })
                context.Entry(tag).State = EntityState.Unchanged;

            // Super fragile check to see if data already exists in the database, and therefore it shouldn't be seeded
            if (context.Users.Any() || context.Projects.Any()) return;

            // NOTE: The id numbers are provided and quite big since we don't want our user ids clobbered by the IdentityProvider 
            var userDave   = new User() { FirstName = "Dave",   LastName = "Bobbart", Email = "dab12@example.com", Id=10001  };
            var userAlex   = new User() { FirstName = "Alex",   LastName = "Johnson", Email = "alj52@example.com", Id=10002  };
            var userSophie = new User() { FirstName = "Sophie", LastName = "Maxwell", Email = "som86@example.com", Id=10003  };
            var userNikau  = new User() { FirstName = "Nikau",  LastName = "Alber",   Email = "nia41@example.com", Id=10004  };
            var userTim    = new User() { FirstName = "Tim",    LastName = "Oberj",   Email = "tio30@example.com", Id=10005  }; 
            var userBob    = new User() { FirstName = "Bob",    LastName = "Davart",  Email = "bod76@example.com", Id=10006  };
            var userJonah  = new User() { FirstName = "Jonah",  LastName = "Olison",  Email = "joo04@example.com", Id=10007  };
            var userBillie = new User() { FirstName = "Billie", LastName = "Jacobs",  Email = "bij25@example.com", Id=10008  };
            var userTamiko = new User() { FirstName = "Tamiko", LastName = "Vice",    Email = "tav92@example.com", Id=10009  };
            var userAmir   = new User() { FirstName = "Amir",   LastName = "Placard", Email = "amp63@example.com", Id=100010 }; 
            var userEmily  = new User() { FirstName = "Emily",  LastName = "Crim",    Email = "emc28@example.com", Id=100011 };
            var userJames  = new User() { FirstName = "James",  LastName = "Ocean",   Email = "jao94@example.com", Id=100012 }; 

            context.Users.Add(userDave);
            context.Users.Add(userAlex);
            context.Users.Add(userSophie);
            context.Users.Add(userNikau);
            context.Users.Add(userTim);
            context.Users.Add(userBob);
            context.Users.Add(userJonah);
            context.Users.Add(userBillie);
            context.Users.Add(userTamiko);
            context.Users.Add(userAmir);
            context.Users.Add(userEmily);
            context.Users.Add(userJames);
            await context.SaveChangesAsync();

            var project = new Project() { 
                Name        = "Very cool project", 
                Description = "This is quite cool :^)", 
                StartDate   = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
                EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(120)),
                Created     = DateTime.Now,
                Creator     = userDave,   
                IsSeedDataProject = true  
            };

            var project2 = new Project() { 
                Name        = "Another project", 
                Description = "The second project belongig to dev man", 
                StartDate   = new System.DateOnly(2021, 11, 15),
                EndDate     = new System.DateOnly(2021, 11, 25),
                Created     = DateTime.Now,
                Creator     = userDave,
                IsSeedDataProject = true        
            };
            context.Projects.AddRange(project, project2);
            await context.SaveChangesAsync();

            // Current (ongoing) sprint for project 1
            var ongoingSprint = new Sprint() { 
                Name = "A different demo sprint", 
                Stage = SprintStage.Started,
                StartDate = project.StartDate, 
                EndDate = project.StartDate.AddDays(100),                
                TimeStarted = project.StartDate.ToDateTime(TimeOnly.MinValue),
                Created = DateTime.Now,
                Creator = userAmir,
            };
            var ongoingStory =  new UserStory() { 
                Project = project,
                StoryGroup = ongoingSprint,
                Creator = userTamiko,
                Created = DateTime.Now,
                Stage = Stage.Todo, 
                Estimate = 13, 
                Name = "Registering and logging into an individual account", 
                Description = "As a user, I want to have an individual account with at least my full name*, nickname, bio, email address*, date of birth*, phone number, and home address* (a textarea will do for this story).",
                AcceptanceCriterias = new List<AcceptanceCriteria>(),
            };
            ongoingSprint.AddStory(ongoingStory);

            ongoingSprint.OverheadEntries = new List<OverheadEntry>();
            ongoingSprint.OverheadEntries.Add(new OverheadEntry() {
                Created = DateTime.Now,
                Description = "I attended my first stand-up!",
                Duration = DurationUtils.TimeSpanFromDurationString("15m").Value,
                Occurred = DateTime.Now,
                Session = standupSession,
                Sprint = ongoingSprint,
                User = userAlex
            });

            await context.SaveChangesAsync();
            
            List<AcceptanceCriteria> ongoingAcceptanceCriteria = new List<AcceptanceCriteria>() {
                new AcceptanceCriteria() { InStoryId = 1,  Content = "Assuming I am not already logged in, the application gives me the ability to either log in or register (create) a new account. When registering, the mandatory attributes are clearly marked. "},
                new AcceptanceCriteria() { InStoryId = 2,  Content = "The username I use to log in should be my email address that I have previously registered in the system. If I try to register an account with an email address that is already registered, the system should not create the account but let me know. Similarly, if I try to log in with an email address that has not been registered, the system should let me know."},
                new AcceptanceCriteria() { InStoryId = 3,  Content = "If when logging in, my details are incorrect (username/password), the system should generate an error message letting me know."},
                new AcceptanceCriteria() { InStoryId = 4,  Content = "Appropriate validation is carried out and errors are clearly conveyed."},
                new AcceptanceCriteria() { InStoryId = 5,  Content = "When I tab, I cycle through the form fields in the correct order (i.e. pressing tab should take me to the next form item that I need to fill in). This needs to always be true even in future stories."},
                new AcceptanceCriteria() { InStoryId = 6,  Content = "Appropriate error messages should be shown (e.g. mandatory field not filled in). The error message should help me understand the problem and the location of the problem so that I can easily fix it."},
                new AcceptanceCriteria() { InStoryId = 7,  Content = "Passwords are not stored in plain text."},
                new AcceptanceCriteria() { InStoryId = 8,  Content = "On successful log-in or registration, I am taken to my own profile page within the system. Currently, the profile page simply displays my profile info formatted to be easily readable."},
                new AcceptanceCriteria() { InStoryId = 9,  Content = "On the profile page, it shows the date of registration and in brackets the years and months (rounded down to the nearest month) since registration, e.g. “Member since: 2 April 2020 (10 months)”. "},
                new AcceptanceCriteria() { InStoryId = 10, Content = "My data (user profile) is stored persistently so that I can log in at another time. For now, go for the easiest/simplest solution to do this."},
                new AcceptanceCriteria() { InStoryId = 11, Content = "I can log out. Pressing the browser’s back button or going directly to my profile’s URL does not show my data. Doing so will redirect to the log-in/sign-in page. "},
                new AcceptanceCriteria() { InStoryId = 12, Content = "I can log back into my profile with my username (email address) and password. Appropriate error messages are shown for unsuccessful log-ins."},
                new AcceptanceCriteria() { InStoryId = 13, Content = "For demo purposes, all attributes can be shown on the profile page. We will change this in future stories so that only some attributes can be seen by others. "},
            };

            foreach (AcceptanceCriteria criteria in ongoingAcceptanceCriteria) {
                ongoingStory.AcceptanceCriterias.Add(criteria);
            }         

            var ongoingTask = new UserStoryTask() { 
                Name="Create and style registration form" , 
                Description="The registration form must be responsive and have validation as per the story ACs", 
                Tags=new List<UserStoryTaskTag>(),
                Created=DateTime.Now, 
                Creator=userDave,
                Priority=Priority.High,
                Stage=Stage.Todo,
                Estimate = TimeSpan.FromHours(2),
                UserStory = ongoingStory,
                OriginalEstimate = TimeSpan.FromHours(2)
            };
            ongoingTask.UserStory = ongoingStory;
            ongoingStory.Tasks.Add(ongoingTask);
            userDave.AssignTask(ongoingTask);

            var ongoingTask2 = new UserStoryTask() { 
                Name="Create registration endpoint in backend" , 
                Description="The registration endpoint is specified in the API spec: https://api.website.com/here", 
                Tags=new List<UserStoryTaskTag>(),
                Created=DateTime.Now, 
                Creator=userNikau,
                Priority=Priority.Critical,
                Stage=Stage.Todo,
                Estimate = TimeSpan.FromHours(3),
                UserStory = ongoingStory,
                OriginalEstimate = TimeSpan.FromHours(3)
            };         

            for (var i=0; i < 100; i++) {
                ongoingTask.Worklog.Add(new WorklogEntry() { 
                    User = userAlex,
                    PairUser = userTamiko,
                    Task = ongoingTask, 
                    Description = "description", 
                    TaggedWorkInstances = new List<TaggedWorkInstance>
                    {
                        new(){ WorklogTagId = fixTag.Id, Duration = TimeSpan.FromMinutes(i)}, 
                        new(){ WorklogTagId = featureTag.Id, Duration = TimeSpan.FromMinutes(i)}, 

                    },
                    Created = DateTime.Now, 
                    Occurred = DateTime.Now.AddDays(i)
                });
            }

            for (var i=0; i < 20; i++) {
                ongoingTask.Worklog.Add(new WorklogEntry() { 
                    User = userTim,                   
                    Task = ongoingTask, 
                    Description = "description :)", 
                    TaggedWorkInstances = new List<TaggedWorkInstance>
                    {
                        new(){ WorklogTagId = documentTag.Id, Duration = TimeSpan.FromMinutes(i)}, 

                    },
                    Created = DateTime.Now, 
                    Occurred = DateTime.Now.AddDays(i)
                });
            }

            ongoingTask2.UserStory = ongoingStory;         
            ongoingStory.Tasks.Add(ongoingTask2);
            userNikau.AssignTask(ongoingTask2);


            ongoingSprint.AddStory(ongoingStory);
            project.Sprints.Add(ongoingSprint);
            await context.SaveChangesAsync();
            /////

            project.MemberAssociations.Add(new ProjectUserMembership() { User = userDave, Role = ProjectRole.Guest });
            project.MemberAssociations.Add(new ProjectUserMembership() { User = userAlex, Role = ProjectRole.Leader });
            project.MemberAssociations.Add(new ProjectUserMembership() { User = userSophie, Role = ProjectRole.Reviewer });
            project.MemberAssociations.Add(new ProjectUserMembership() { User = userNikau, Role = ProjectRole.Guest });
            project.MemberAssociations.Add(new ProjectUserMembership() { User = userTim, Role = ProjectRole.Developer });
            project.MemberAssociations.Add(new ProjectUserMembership() { User = userBob, Role = ProjectRole.Reviewer });
            project.MemberAssociations.Add(new ProjectUserMembership() { User = userJonah, Role = ProjectRole.Guest });
            project.MemberAssociations.Add(new ProjectUserMembership() { User = userTamiko, Role = ProjectRole.Developer });
            project.MemberAssociations.Add(new ProjectUserMembership() { User = userAmir, Role = ProjectRole.Guest });         

            // Add archived sprint
            var archivedSprint = new Sprint() { 
                Name = "Another sprint", 
                StartDate = project.StartDate.AddDays(-7), 
                TimeStarted = project.StartDate.AddDays(-7).ToDateTime(TimeOnly.MinValue),
                EndDate = project.StartDate.AddDays(-6), 
                Stage = SprintStage.Closed,
                Created = DateTime.Now, 
                Creator = userAmir 
            };
            await context.SaveChangesAsync();

            int[] estimates = new int[] { 1, 5, 8, 13, 20 };
            for (int i = 0; i < 50; i++) {
                Random random = new();

                archivedSprint.AddStory(new UserStory() { 
                    Name = $"Finished story {i}",   
                    Estimate = estimates[random.Next(estimates.Count())],  
                    Description = "Story that is done",         
                    Stage = Stage.Done,
                    Creator = userDave,
                    Created = DateTime.Now,
                    StoryGroup = archivedSprint,
                    Project = project,
                    AcceptanceCriterias = new List<AcceptanceCriteria>()
                    {
                        new() { InStoryId = 1, Content = "This is the first acceptance criteria for this story. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Eligendi non quis exercitatione." },
                    }
                });
            }

            project.Sprints.Add(archivedSprint);
            await context.SaveChangesAsync();

            // Archived sprint changelog entries
            for (int i = 0; i < 100; i++) {
                context.SprintChangelogEntries.Add(new(userAlex, archivedSprint, nameof(Sprint.Name), Change<object>.Update("Some Value", "New Value")));
            }
            await context.SaveChangesAsync();

            // Backlog stories
            project.Backlog.AddStory(new UserStory() { 
                Name = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. In in cursus sem. Morbi iaculis finibus ut.", 
                Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent fermentum vehicula lacus, eu bibendum dolor sollicitudin eget. Nullam euismod ultricies porta. Maecenas porta metus et ullamcorper mi.", 
                Stage = Stage.Todo, 
                Estimate = 13,
                Priority = Priority.Low,
                Created = DateTime.Now,
                Creator = userDave,
                StoryGroup = project.Backlog,
                Project = project,
                AcceptanceCriterias = new List<AcceptanceCriteria>()
                {
                    new() { InStoryId = 1, Content = "This is the first acceptance criteria for this story. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Eligendi non quis exercitatione." },
                }
            });
            project.Backlog.AddStory(new UserStory() { 
                Name = "Test story 3", 
                Description = "This is the third story", 
                Stage = Stage.Todo, 
                Estimate = 40,
                Priority = Priority.Critical,
                Created = DateTime.Now,
                Creator = userDave,
                StoryGroup = project.Backlog,
                Project = project,
                AcceptanceCriterias = new List<AcceptanceCriteria>()
                {
                    new() { InStoryId = 1, Content = "This is the first acceptance criteria for this story. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Eligendi non quis exercitatione." },
                }
            });
            project.Backlog.AddStory(new UserStory() { 
                Name = "Test story 4", 
                Description = "This is the fourth story", 
                Stage = Stage.Todo, 
                Estimate = 5,
                Priority = Priority.Normal,
                Created = DateTime.Now,
                Creator = userDave,
                StoryGroup = project.Backlog,
                Project = project,
                AcceptanceCriterias = new List<AcceptanceCriteria>()
                {
                    new() { InStoryId = 1, Content = "This is the first acceptance criteria for this story. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Eligendi non quis exercitatione." },
                }
            });
            project.Backlog.AddStory(new UserStory() { 
                Name = "Test story 5", 
                Description = "This is the fifth story",
                Stage = Stage.Todo, 
                Estimate = 8,
                Priority = Priority.High,
                Created = DateTime.Now,
                Creator = userDave,
                StoryGroup = project.Backlog,
                Project = project,
                AcceptanceCriterias = new List<AcceptanceCriteria>()
                {
                    new() { InStoryId = 1, Content = "This is the first acceptance criteria for this story. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Eligendi non quis exercitatione." },
                }
            });

            await context.SaveChangesAsync();

            await SeedBurndownProject(context);
            await SeedReviewProject(context);
        }

        public async Task SeedBurndownProject(DatabaseContext context)
        {
            var user = context.Users.First();
            var created = DateTime.Now.AddDays(-10);
            var project = new Project() {
                Name        = "Very cool burndown project", 
                Description = "The burndown is quite cool :^)", 
                StartDate   = DateOnly.FromDateTime(created.AddDays(-11)),
                EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                Created     = created,
                Creator     = user,
                IsSeedDataProject = true
            };
            project.MemberAssociations.Add(new() { User = user, Role = ProjectRole.Leader});
            context.Projects.Add(project);
            await context.SaveChangesAsync();

            // Current burndown sprint
            {
                var sprint = new Sprint() {
                    Project     = project,
                    Name        = "Sprint with cool burndown",
                    Created     = created,
                    TimeStarted = created,
                    StartDate   = DateOnly.FromDateTime(created),
                    EndDate     = DateOnly.FromDateTime(created.AddDays(9)),
                    Stage       = SprintStage.Started,
                    Creator     = user,
                };

                context.Sprints.Add(sprint);
                await context.SaveChangesAsync();

                var story = new UserStory() {
                    Project = project,
                    StoryGroup = sprint,
                    Name = "See sprint burndown",
                    Description = "As Nikau, I want to see the sprint burndown so that I know where the team's at during a sprint.",
                    Creator = user,
                    Created = created,
                    AcceptanceCriterias = new List<AcceptanceCriteria>()
                    {
                        new() { InStoryId = 1, Content = "There is a dedicated place where I can browse for reporting features all at one place. One of these options is to show the burndown charts for the current project." },
                        new() { InStoryId = 2, Content = "By default, I'm presented with the burndown of the current sprint (once the sprint has started) and I can select any other completed sprint for the current project. Sprints that are not started will not be available to select." },
                        new() { InStoryId = 3, Content = "The burndown is presented as a 2D line graph with total hours on the Y-axis and days on the X-axis." },
                        new() { InStoryId = 4, Content = "There is an \"ideal\" straight line starting from the total number of estimated hours descending to 0 hours on the last day of the sprint." },
                        new() { InStoryId = 5, Content = "There is an \"actual progress\" line showing all hours already spent on the project so that the line decrease of the amount of each work log that is recorded." },
                        new() { InStoryId = 6, Content = "The \"actual progress\" also show scope changes when tasks are re-estimated or stories are added. Re-estimation prior the sprint has started are not considered scope changes." },
                        new() { InStoryId = 7, Content = "When hovering the \"actual progress\", I can see some details about the work logs and related tasks logged at that specific hovered time." },
                    }
                };

                context.UserStories.Add(story);
                await context.SaveChangesAsync();

                var task1 = new UserStoryTask() {
                    Name = "First task",
                    Description = "Is first task",
                    Stage = Stage.InProgress,
                    OriginalEstimate = TimeSpan.FromHours(2),
                    Estimate = TimeSpan.FromHours(3.5),
                    Created = created,
                    Creator = user,
                    UserStory = story,
                    Tags=new List<UserStoryTaskTag>()
                };


                var task2 = new UserStoryTask() {
                    Name = "Second task",
                    Description = "Is second task",
                    OriginalEstimate = TimeSpan.FromHours(4),
                    Estimate = TimeSpan.FromHours(4),
                    Created = created.AddDays(-0.2),
                    Creator = user,
                    UserStory = story,
                    Tags = new List<UserStoryTaskTag>()
                };

                context.UserStoryTasks.Add(task1);
                context.UserStoryTasks.Add(task2);
                await context.SaveChangesAsync();
                
                foreach (var i in Enumerable.Range(1, 10)) {
                    var worklogEntry = new WorklogEntry() {
                        Task = task1,
                        Created = created.AddDays(i), 
                        Occurred = created.AddDays(i),
                        Description = $"Worklog entry: {i}",
                        User = user,
                        TaggedWorkInstances = new List<TaggedWorkInstance>
                        {
                            new(){ WorklogTagId = fixTag.Id, Duration = TimeSpan.FromMinutes(30)}, 

                        },
                    };
                    context.WorklogEntries.Add(worklogEntry);
                }
                
                foreach (var i in Enumerable.Range(1, 3)) {
                    var worklogEntry = new WorklogEntry() {
                        Task = task2,
                        Created = created.AddDays(0.75 + i * 2),
                        Occurred = created.AddDays(0.75 + i * 2),
                        Description = $"Worklog entry: {i}",
                        User = user,
                        TaggedWorkInstances = new List<TaggedWorkInstance>
                        {
                            new(){ WorklogTagId = i % 2 == 0 ? featureTag.Id : spikeTag.Id, Duration = TimeSpan.FromMinutes(90)}, 

                        },
                    };
                    context.WorklogEntries.Add(worklogEntry);
                }

                var changelogEntry = new UserStoryTaskChangelogEntry(user, task1, nameof(UserStoryTask.Estimate), Change<object>.Update(task1.OriginalEstimate, task1.Estimate));
                changelogEntry.Created = created.AddDays(5.5);
                context.Add(changelogEntry);
                
                changelogEntry = new UserStoryTaskChangelogEntry(user, task1, nameof(UserStoryTask.Stage), Change<object>.Update(task1.Stage, Stage.Deferred));
                changelogEntry.Created = created.AddDays(5.75);
                context.Add(changelogEntry);
                
                changelogEntry = new UserStoryTaskChangelogEntry(user, task1, nameof(UserStoryTask.Stage), Change<object>.Update(Stage.Deferred, task1.Stage));
                changelogEntry.Created = created.AddDays(6.2);
                context.Add(changelogEntry);

                await context.SaveChangesAsync();
            }
            // Previous burndown sprint
            {
                created = created.AddDays(-10);
                var sprint = new Sprint() {
                    Project     = project,
                    Name        = "Sprint with cool flow diagram",
                    Created     = created,
                    TimeStarted = created,
                    StartDate   = DateOnly.FromDateTime(created),
                    EndDate     = DateOnly.FromDateTime(created.AddDays(8)),
                    Stage       = SprintStage.Closed,
                    Creator     = user,
                };
                context.Sprints.Add(sprint);
                await context.SaveChangesAsync();
                
                var story = new UserStory() {
                    Project = project,
                    StoryGroup = sprint,
                    Name = "Visualise flow diagram",
                    Description = "As Nikau, I want to see a cumulative flow diagram showing the proportion of tasks staying in each status during the project so that I understand how my project doing is going in terms of its process.",
                    Creator = user,
                    Created = created,
                    AcceptanceCriterias = new List<AcceptanceCriteria>()
                    {
                        new() { InStoryId = 1, Content = "Via a dedicated UI element where all reporting features are, I can see the cumulative flow diagram of current project." },
                        new() { InStoryId = 2, Content = "I can toggle between the following views/filters:\n * Project - to see the flow over the whole project\n * Sprint - to see one particular sprint" },
                        new() { InStoryId = 3, Content = "A cumulative flow chart shows the sum of remaining time estimate for each task in each status on the Y-axis and the time on the X-axis. Each status has a different colour." },
                        new() { InStoryId = 4, Content = "When hovering on a line, I can see the logged time and related tasks / stories in a tooltip." },
                    }
                };

                context.UserStories.Add(story);
                await context.SaveChangesAsync();
                
                var task1 = new UserStoryTask() {
                    Name = "Chart mockup",
                    Description = "Design mockup for chart",
                    Stage = Stage.Done,
                    OriginalEstimate = TimeSpan.FromHours(1),
                    Estimate = TimeSpan.FromHours(1),
                    Created = created,
                    Creator = user,
                    UserStory = story,
                    Tags=new List<UserStoryTaskTag>() { },
                };

                var task2 = new UserStoryTask() {
                    Name = "Create chart",
                    Description = "Is second task",
                    Stage = Stage.Done,
                    OriginalEstimate = TimeSpan.FromHours(4),
                    Estimate = TimeSpan.FromHours(4),
                    Created = created.AddDays(-0.1),
                    Creator = user,
                    UserStory = story,
                    Tags = new List<UserStoryTaskTag>(),
                };
                
                var task3 = new UserStoryTask() {
                    Name = "Fix Chart",
                    Description = "Is second task",
                    Stage = Stage.Done,
                    OriginalEstimate = TimeSpan.FromHours(4),
                    Estimate = TimeSpan.FromHours(4),
                    Created = created.AddDays(3.4),
                    Creator = user,
                    UserStory = story,
                    Tags = new List<UserStoryTaskTag>(),
                };
                
                context.UserStoryTasks.AddRange(task1, task2, task3);
                await context.SaveChangesAsync();

                var stages = new[] {Stage.Todo, Stage.InProgress, Stage.UnderReview, Stage.Done};

                var step = 1;
                foreach (var (oldStage, newStage) in stages.Zip(stages.Skip(1)))
                {
                    var change1 = new UserStoryTaskChangelogEntry(user, task1, nameof(UserStoryTask.Stage),
                        Change<object>.Update(oldStage, newStage));
                    change1.Created = task1.Created + TimeSpan.FromDays(1) * step;
                    context.UserStoryTaskChangelogEntries.Add(change1);
                    
                    var change2 = new UserStoryTaskChangelogEntry(user, task2, nameof(UserStoryTask.Stage),
                        Change<object>.Update(oldStage, newStage));
                    change2.Created = task2.Created + TimeSpan.FromDays(1.5) * step;
                    context.UserStoryTaskChangelogEntries.Add(change2);
                    
                    var change3 = new UserStoryTaskChangelogEntry(user, task3, nameof(UserStoryTask.Stage),
                        Change<object>.Update(oldStage, newStage));
                    change3.Created = task3.Created + TimeSpan.FromDays(0.75) * step;
                    context.UserStoryTaskChangelogEntries.Add(change3);

                    step++;
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task SeedReviewProject(DatabaseContext context)
        {
            var inProjectStoryId = 1;

            var user = context.Users.First();
            var created = DateTime.Now.AddDays(-10);
            var project = new Project() {
                Id          = 200,
                Name        = "Very cool review project", 
                Description = "Much review, so story", 
                StartDate   = DateOnly.FromDateTime(created),
                EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                Created     = created,
                Creator     = user,
                IsSeedDataProject = true
            };
            project.MemberAssociations.Add(new() { User = user, Role = ProjectRole.Leader});
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            
            var sprint = new Sprint() {
                Project     = project,
                Name        = "Sprint with cool review",
                Created     = created,
                TimeStarted = created,
                StartDate   = DateOnly.FromDateTime(created),
                EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                Stage       = SprintStage.ReadyToReview,
                Creator     = user,
            };
            context.Sprints.Add(sprint);
            await context.SaveChangesAsync();

            var stories = new List<UserStory>();
            stories.Add(new UserStory() {
                Order = inProjectStoryId++,
                Project = project,
                StoryGroup = sprint,
                Stage = Stage.Done,
                Estimate = 13,
                Priority = Priority.Low,
                Name = "Search",
                Description = "As a casual user, I’d like to be able to see only those events whose title contains some characters, words, or a phrase, so that I can find the ones that are of most interest to me.",
                Creator = user,
                Created = created,
                AcceptanceCriterias = new List<AcceptanceCriteria>()
                {
                    new() { InStoryId = 1, Content = "The user can type a word or phrase into an appropriate search box to search for specific events." },
                    new() { InStoryId = 2, Content = "Only, and all, events whose title contains the provided word or phrase are shown (possibly after using pagination)." },
                    new() { InStoryId = 3, Content = "The results should be shown as described in story 2 with the possibility of being filtered as described in story 3." },
                },
            });
            stories.Add(new UserStory() {
                Order = inProjectStoryId++,
                Project = project,
                StoryGroup = sprint,
                Stage = Stage.Done,
                Estimate = 8,
                Priority = Priority.High,
                Name = "List of events",
                Description = "As a casual user, I’d like to be able to see a list of the existing events yet to be happening.",
                Creator = user,
                Created = created,
                AcceptanceCriterias = new List<AcceptanceCriteria>()
                {
                    new() { InStoryId = 1, Content = "Basic information about each event should be visible, composed of: \nHero image;\n Date and time;\n Title;\n Category;\n Host (name and hero image);\n Number of attendees." },
                    new() { InStoryId = 2, Content = "All events should be shown (possibly after using pagination)." },
                },
            });
            stories.Add(new UserStory() {
                Order = inProjectStoryId++,
                Project = project,
                StoryGroup = sprint,
                Stage = Stage.Done,
                Estimate = 13,
                Priority = Priority.High,
                Name = "Filter",
                Description = "As a casual user, I’d like to be able to filter events shown to those of that match a set of categories.",
                Creator = user,
                Created = created,
                AcceptanceCriterias = new List<AcceptanceCriteria>()
                {
                    new() { InStoryId = 1, Content = "The user can select one or more option (eg, category film) of the category filter to filter by." },
                    new() { InStoryId = 2, Content = "The user can use no filter or filter by category" },
                    new() { InStoryId = 3, Content = "Only, and all, events in the selected category(ies) should be shown (possibly after using pagination)." },
                },
            });
            stories.Add(new UserStory() {
                Order = inProjectStoryId++,
                Project = project,
                StoryGroup = sprint,
                Stage = Stage.Done,
                Name = "Sort",
                Description = "As a casual user, I’d like to be able to sort the events.",
                Creator = user,
                Created = created,
                AcceptanceCriterias = new List<AcceptanceCriteria>()
                {
                    new() { InStoryId = 1, Content = "By default, events must be ordered according to their date, from the first to be happening to the latest." },
                    new() { InStoryId = 2, Content = "The user can choose to sort them one of the following ways:\n Ascending by number of attendees;\n Descending by number of attendees;\n Chronologically by date, from the latest to the first to be happening;\n Chronologically by date, from the first to the latest to be happening." },
                },
            });
            stories.Add(new UserStory() {
                Order = inProjectStoryId++,
                Project = project,
                StoryGroup = sprint,
                Stage = Stage.Done,
                Name = "Pagination",
                Description = "As a casual user, I’d like to see the events shown in batches.",
                Creator = user,
                Created = created,
                AcceptanceCriterias = new List<AcceptanceCriteria>()
                {
                    new() { InStoryId = 1, Content = "If there are more than 10 events in the list, then the user should only see the first 10 to begin with, ie events 1-10." },
                    new() { InStoryId = 2, Content = "The user can choose to view the next batch of 10 (if there are additional events), ie events 11-20. In this way, the user should be able to look through all the events 10 at a time." },
                    new() { InStoryId = 3, Content = "The user can choose to view the previous batch of 10, where the user has progressed beyond the first 10 events. For example, if the user is viewing events 21-30, they can ‘page back’ to events 11-20." },
                    new() { InStoryId = 4, Content = "The user can choose to progress to the first page (ie events 1-10), if they are not already on that page." },
                    new() { InStoryId = 5, Content = "The user can choose to progress to the last page if they are not already there." },
                    new() { InStoryId = 6, Content = "The user should be able to see the index of the current page, where the index starts at 1. Events 1-10 are on page 1, 11-20 on page 2, etc." },
                    new() { InStoryId = 7, Content = "The user should be able to see when there are no more events - there should be an indication that the last page has been reached." },
                    new() { InStoryId = 8, Content = "The last batch may contain less than 10 events. For example, if there are 25 petitions, the pages should contain petitions 1-10, 11-20, and 21-25." },
                }
            });
            stories.Add(new UserStory() {
                Order = inProjectStoryId,
                Project = project,
                StoryGroup = sprint,
                Stage = Stage.Done,
                Name = "Combination",
                Description = "As a casual user, I’d like to be able to combine searching, filtering, and sorting.",
                Creator = user,
                Created = created,
                AcceptanceCriterias = new List<AcceptanceCriteria>()
                {
                    new() { InStoryId = 1, Content = "The user should be able to select multiple options, and this should result in them all being applied. For example, if the user searches for “project management”, filters by study-group category, and sorts by number of attendees, then only events with “project management” in the title and with study-group in the category should be displayed, sorted by their number of attendees." },
                },
            });

            context.UserStories.AddRange(stories);
            await context.SaveChangesAsync();

            // Add closed sprint
            var closedSprint = new Sprint() { 
                Name = "A closed sprint",
                TimeStarted = created.AddDays(1),
                StartDate   = DateOnly.FromDateTime(created),
                EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                Stage = SprintStage.Closed,
                Created = DateTime.Now, 
                Creator = user 
            };
            await context.SaveChangesAsync();

            int[] estimates = new int[] { 1, 5, 8, 13, 20 };
            for (int i = 0; i < 8; i++) {
                Random random = new();

                closedSprint.AddStory(new UserStory() { 
                    Name = $"Finished story {i}",   
                    Estimate = estimates[random.Next(estimates.Count())],  
                    Description = "Story that is done",         
                    Stage = Stage.Done,
                    Creator = user,
                    Created = DateTime.Now,
                    StoryGroup = closedSprint,
                    Project = project,
                    AcceptanceCriterias = new List<AcceptanceCriteria>()
                    {
                        new() { InStoryId = 1, Content = "This is the first acceptance criteria for this story. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Eligendi non quis exercitatione." },
                    }
                });
            }

            project.Sprints.Add(closedSprint);
            await context.SaveChangesAsync();
        }
    }
}