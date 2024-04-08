using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrumBoard.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FriendlyName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Xml = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GitlabCommits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(95)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WebUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuthorName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuthorEmail = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuthoredDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitlabCommits", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OverheadSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverheadSessions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FirstName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LDAPUsername = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserStoryTaskTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Style = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStoryTaskTags", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorklogTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Style = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorklogTags", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatorId = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastEditorId = table.Column<long>(type: "bigint", nullable: false),
                    LastEdited = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Start = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    End = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CanBeHidden = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ManuallyArchived = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Announcements_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Announcements_Users_LastEditorId",
                        column: x => x.LastEditorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FormTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(95)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatorId = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RunNumber = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormTemplates_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    GitlabCredentials_GitlabURL = table.Column<string>(type: "varchar(95)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GitlabCredentials_Id = table.Column<long>(type: "bigint", nullable: true),
                    GitlabCredentials_AccessToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GitlabCredentials_PushWebhookSecretToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatorId = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn),
                    IsSeedDataProject = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SinglePerUserFlags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    FlagType = table.Column<int>(type: "int", nullable: false),
                    IsSet = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinglePerUserFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SinglePerUserFlags_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AnnouncementHides",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AnnouncementId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementHides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnouncementHides_Announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "Announcements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnnouncementHides_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FormTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RunNumber = table.Column<long>(type: "bigint", nullable: false),
                    AllowSavingBeforeStartDate = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_FormTemplates_FormTemplateId",
                        column: x => x.FormTemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FormTemplateBlocks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FormTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    FormPosition = table.Column<long>(type: "bigint", nullable: false),
                    Discriminator = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Prompt = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Required = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    AllowMultiple = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    MaxResponseLength = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormTemplateBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormTemplateBlocks_FormTemplates_FormTemplateId",
                        column: x => x.FormTemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProjectFeatureFlags",
                columns: table => new
                {
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    FeatureFlag = table.Column<int>(type: "int", nullable: false),
                    CreatorId = table.Column<long>(type: "bigint", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFeatureFlags", x => new { x.ProjectId, x.FeatureFlag });
                    table.ForeignKey(
                        name: "FK_ProjectFeatureFlags_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFeatureFlags_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProjectUserMemberships",
                columns: table => new
                {
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectUserMemberships", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ProjectUserMemberships_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectUserMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StoryGroup",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Discriminator = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BacklogProjectId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatorId = table.Column<long>(type: "bigint", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TimeStarted = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Stage = table.Column<int>(type: "int", nullable: true),
                    SprintProjectId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryGroup_Projects_BacklogProjectId",
                        column: x => x.BacklogProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoryGroup_Projects_SprintProjectId",
                        column: x => x.SprintProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoryGroup_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserStandUpCalendarLinks",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    Token = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStandUpCalendarLinks", x => new { x.UserId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_UserStandUpCalendarLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStandUpCalendarLinks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FormInstances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProjectId = table.Column<long>(type: "bigint", nullable: true),
                    AssignmentId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Discriminator = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LinkedProjectId = table.Column<long>(type: "bigint", nullable: true),
                    AssigneeId = table.Column<long>(type: "bigint", nullable: true),
                    PairId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormInstances_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormInstances_Projects_LinkedProjectId",
                        column: x => x.LinkedProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormInstances_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FormInstances_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormInstances_Users_PairId",
                        column: x => x.PairId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MultiChoiceOption",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BlockId = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiChoiceOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultiChoiceOption_FormTemplateBlocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "FormTemplateBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OverheadEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Occurred = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DurationTicks = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SprintId = table.Column<long>(type: "bigint", nullable: false),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverheadEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OverheadEntries_OverheadSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "OverheadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OverheadEntries_StoryGroup_SprintId",
                        column: x => x.SprintId,
                        principalTable: "StoryGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OverheadEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StandUpMeetings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SprintId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScheduledStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualStart = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    CreatorId = table.Column<long>(type: "bigint", nullable: false),
                    StartedById = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandUpMeetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandUpMeetings_StoryGroup_SprintId",
                        column: x => x.SprintId,
                        principalTable: "StoryGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandUpMeetings_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandUpMeetings_Users_StartedById",
                        column: x => x.StartedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserStories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Order = table.Column<long>(type: "bigint", nullable: false),
                    InProjectId = table.Column<long>(type: "bigint", nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    StoryGroupId = table.Column<long>(type: "bigint", nullable: false),
                    CreatorId = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estimate = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    ReviewComments = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStories_StoryGroup_StoryGroupId",
                        column: x => x.StoryGroupId,
                        principalTable: "StoryGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStories_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Answers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    FormInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    Discriminator = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Answer = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Answers_FormInstances_FormInstanceId",
                        column: x => x.FormInstanceId,
                        principalTable: "FormInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Answers_FormTemplateBlocks_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "FormTemplateBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StandUpMeetingAttendance",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    StandUpMeetingId = table.Column<long>(type: "bigint", nullable: false),
                    ArrivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandUpMeetingAttendance", x => new { x.UserId, x.StandUpMeetingId });
                    table.ForeignKey(
                        name: "FK_StandUpMeetingAttendance_StandUpMeetings_StandUpMeetingId",
                        column: x => x.StandUpMeetingId,
                        principalTable: "StandUpMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandUpMeetingAttendance_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AcceptanceCriterias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InStoryId = table.Column<long>(type: "bigint", nullable: false),
                    UserStoryId = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn),
                    Status = table.Column<int>(type: "int", nullable: true),
                    ReviewComments = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptanceCriterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcceptanceCriterias_UserStories_UserStoryId",
                        column: x => x.UserStoryId,
                        principalTable: "UserStories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserStoryTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InProjectId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatorId = table.Column<long>(type: "bigint", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Complexity = table.Column<int>(type: "int", nullable: false),
                    OriginalEstimateTicks = table.Column<long>(type: "bigint", nullable: false),
                    EstimateTicks = table.Column<long>(type: "bigint", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    UserStoryId = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<DateTime>(type: "timestamp(6)", rowVersion: true, nullable: true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStoryTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStoryTasks_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStoryTasks_UserStories_UserStoryId",
                        column: x => x.UserStoryId,
                        principalTable: "UserStories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MultichoiceAnswerMultichoiceOption",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MultichoiceAnswerId = table.Column<long>(type: "bigint", nullable: false),
                    MultichoiceOptionId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultichoiceAnswerMultichoiceOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultichoiceAnswerMultichoiceOption_Answers_MultichoiceAnswer~",
                        column: x => x.MultichoiceAnswerId,
                        principalTable: "Answers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultichoiceAnswerMultichoiceOption_MultiChoiceOption_Multich~",
                        column: x => x.MultichoiceOptionId,
                        principalTable: "MultiChoiceOption",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StandUpMeetingTaskCheckIns",
                columns: table => new
                {
                    StandUpMeetingId = table.Column<long>(type: "bigint", nullable: false),
                    TaskId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletionStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandUpMeetingTaskCheckIns", x => new { x.StandUpMeetingId, x.TaskId, x.UserId });
                    table.ForeignKey(
                        name: "FK_StandUpMeetingTaskCheckIns_StandUpMeetings_StandUpMeetingId",
                        column: x => x.StandUpMeetingId,
                        principalTable: "StandUpMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandUpMeetingTaskCheckIns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandUpMeetingTaskCheckIns_UserStoryTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "UserStoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserStoryTaskTagJoins",
                columns: table => new
                {
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    TaskId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStoryTaskTagJoins", x => new { x.TagId, x.TaskId });
                    table.ForeignKey(
                        name: "FK_UserStoryTaskTagJoins_UserStoryTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "UserStoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStoryTaskTagJoins_UserStoryTaskTags_TagId",
                        column: x => x.TagId,
                        principalTable: "UserStoryTaskTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserTaskAssociations",
                columns: table => new
                {
                    TaskId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTaskAssociations", x => new { x.UserId, x.TaskId });
                    table.ForeignKey(
                        name: "FK_UserTaskAssociations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTaskAssociations_UserStoryTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "UserStoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorklogEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TaskId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Occurred = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PairUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorklogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorklogEntries_Users_PairUserId",
                        column: x => x.PairUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorklogEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorklogEntries_UserStoryTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "UserStoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TaggedWorkInstances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WorklogTagId = table.Column<long>(type: "bigint", nullable: false),
                    WorklogEntryId = table.Column<long>(type: "bigint", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaggedWorkInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaggedWorkInstances_WorklogEntries_WorklogEntryId",
                        column: x => x.WorklogEntryId,
                        principalTable: "WorklogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaggedWorkInstances_WorklogTags_WorklogTagId",
                        column: x => x.WorklogTagId,
                        principalTable: "WorklogTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorklogCommitJoins",
                columns: table => new
                {
                    EntryId = table.Column<long>(type: "bigint", nullable: false),
                    CommitId = table.Column<string>(type: "varchar(95)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorklogCommitJoins", x => new { x.CommitId, x.EntryId });
                    table.ForeignKey(
                        name: "FK_WorklogCommitJoins_GitlabCommits_CommitId",
                        column: x => x.CommitId,
                        principalTable: "GitlabCommits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorklogCommitJoins_WorklogEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "WorklogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ChangelogEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatorId = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    FieldChanged = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FromValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EditingSessionGuid = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Discriminator = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OverheadEntryChangedId = table.Column<long>(type: "bigint", nullable: true),
                    OldSessionId = table.Column<long>(type: "bigint", nullable: true),
                    NewSessionId = table.Column<long>(type: "bigint", nullable: true),
                    ProjectChangedId = table.Column<long>(type: "bigint", nullable: true),
                    RelatedUserId = table.Column<long>(type: "bigint", nullable: true),
                    SprintChangedId = table.Column<long>(type: "bigint", nullable: true),
                    UserStoryChangedId = table.Column<long>(type: "bigint", nullable: true),
                    StandUpMeetingChangedId = table.Column<long>(type: "bigint", nullable: true),
                    StandUpMeetingUserMembershipChangelogEntry_RelatedUserId = table.Column<long>(type: "bigint", nullable: true),
                    CheckInStandUpMeetingId = table.Column<long>(type: "bigint", nullable: true),
                    CheckInTaskId = table.Column<long>(type: "bigint", nullable: true),
                    CheckInUserId = table.Column<long>(type: "bigint", nullable: true),
                    UserStoryChangelogEntry_UserStoryChangedId = table.Column<long>(type: "bigint", nullable: true),
                    AcceptanceCriteriaChangedId = table.Column<long>(type: "bigint", nullable: true),
                    UserStoryTaskChangedId = table.Column<long>(type: "bigint", nullable: true),
                    UserStoryTaskTagChangedId = table.Column<long>(type: "bigint", nullable: true),
                    UserChangedId = table.Column<long>(type: "bigint", nullable: true),
                    WorklogEntryChangedId = table.Column<long>(type: "bigint", nullable: true),
                    TaggedWorkInstanceId = table.Column<long>(type: "bigint", nullable: true),
                    WorklogTagId = table.Column<long>(type: "bigint", nullable: true),
                    WorklogEntryId = table.Column<long>(type: "bigint", nullable: true),
                    CommitChangedId = table.Column<string>(type: "varchar(95)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PairUserChangedId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangelogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_AcceptanceCriterias_AcceptanceCriteriaChang~",
                        column: x => x.AcceptanceCriteriaChangedId,
                        principalTable: "AcceptanceCriterias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_GitlabCommits_CommitChangedId",
                        column: x => x.CommitChangedId,
                        principalTable: "GitlabCommits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_OverheadEntries_OverheadEntryChangedId",
                        column: x => x.OverheadEntryChangedId,
                        principalTable: "OverheadEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_OverheadSessions_NewSessionId",
                        column: x => x.NewSessionId,
                        principalTable: "OverheadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_OverheadSessions_OldSessionId",
                        column: x => x.OldSessionId,
                        principalTable: "OverheadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_Projects_ProjectChangedId",
                        column: x => x.ProjectChangedId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_StandUpMeetingTaskCheckIns_CheckInStandUpMe~",
                        columns: x => new { x.CheckInStandUpMeetingId, x.CheckInTaskId, x.CheckInUserId },
                        principalTable: "StandUpMeetingTaskCheckIns",
                        principalColumns: new[] { "StandUpMeetingId", "TaskId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_StoryGroup_SprintChangedId",
                        column: x => x.SprintChangedId,
                        principalTable: "StoryGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_TaggedWorkInstances_TaggedWorkInstanceId",
                        column: x => x.TaggedWorkInstanceId,
                        principalTable: "TaggedWorkInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_Users_PairUserChangedId",
                        column: x => x.PairUserChangedId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_Users_RelatedUserId",
                        column: x => x.RelatedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_Users_StandUpMeetingUserMembershipChangelog~",
                        column: x => x.StandUpMeetingUserMembershipChangelogEntry_RelatedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_Users_UserChangedId",
                        column: x => x.UserChangedId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_UserStories_UserStoryChangedId",
                        column: x => x.UserStoryChangedId,
                        principalTable: "UserStories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_UserStories_UserStoryChangelogEntry_UserSto~",
                        column: x => x.UserStoryChangelogEntry_UserStoryChangedId,
                        principalTable: "UserStories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_UserStoryTasks_UserStoryTaskChangedId",
                        column: x => x.UserStoryTaskChangedId,
                        principalTable: "UserStoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_UserStoryTaskTags_UserStoryTaskTagChangedId",
                        column: x => x.UserStoryTaskTagChangedId,
                        principalTable: "UserStoryTaskTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_WorklogEntries_WorklogEntryChangedId",
                        column: x => x.WorklogEntryChangedId,
                        principalTable: "WorklogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_WorklogEntries_WorklogEntryId",
                        column: x => x.WorklogEntryId,
                        principalTable: "WorklogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangelogEntries_WorklogTags_WorklogTagId",
                        column: x => x.WorklogTagId,
                        principalTable: "WorklogTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceCriterias_UserStoryId",
                table: "AcceptanceCriterias",
                column: "UserStoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementHides_AnnouncementId",
                table: "AnnouncementHides",
                column: "AnnouncementId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementHides_UserId",
                table: "AnnouncementHides",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_CreatorId",
                table: "Announcements",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_LastEditorId",
                table: "Announcements",
                column: "LastEditorId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_FormInstanceId",
                table: "Answers",
                column: "FormInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_FormTemplateId",
                table: "Assignments",
                column: "FormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_AcceptanceCriteriaChangedId",
                table: "ChangelogEntries",
                column: "AcceptanceCriteriaChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_CheckInStandUpMeetingId_CheckInTaskId_Check~",
                table: "ChangelogEntries",
                columns: new[] { "CheckInStandUpMeetingId", "CheckInTaskId", "CheckInUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_CommitChangedId",
                table: "ChangelogEntries",
                column: "CommitChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_CreatorId",
                table: "ChangelogEntries",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_NewSessionId",
                table: "ChangelogEntries",
                column: "NewSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_OldSessionId",
                table: "ChangelogEntries",
                column: "OldSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_OverheadEntryChangedId",
                table: "ChangelogEntries",
                column: "OverheadEntryChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_PairUserChangedId",
                table: "ChangelogEntries",
                column: "PairUserChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_ProjectChangedId",
                table: "ChangelogEntries",
                column: "ProjectChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_RelatedUserId",
                table: "ChangelogEntries",
                column: "RelatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_SprintChangedId",
                table: "ChangelogEntries",
                column: "SprintChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_StandUpMeetingUserMembershipChangelogEntry_~",
                table: "ChangelogEntries",
                column: "StandUpMeetingUserMembershipChangelogEntry_RelatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_TaggedWorkInstanceId",
                table: "ChangelogEntries",
                column: "TaggedWorkInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_UserChangedId",
                table: "ChangelogEntries",
                column: "UserChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_UserStoryChangedId",
                table: "ChangelogEntries",
                column: "UserStoryChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_UserStoryChangelogEntry_UserStoryChangedId",
                table: "ChangelogEntries",
                column: "UserStoryChangelogEntry_UserStoryChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_UserStoryTaskChangedId",
                table: "ChangelogEntries",
                column: "UserStoryTaskChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_UserStoryTaskTagChangedId",
                table: "ChangelogEntries",
                column: "UserStoryTaskTagChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_WorklogEntryChangedId",
                table: "ChangelogEntries",
                column: "WorklogEntryChangedId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_WorklogEntryId",
                table: "ChangelogEntries",
                column: "WorklogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_WorklogTagId",
                table: "ChangelogEntries",
                column: "WorklogTagId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_AssigneeId",
                table: "FormInstances",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_AssignmentId",
                table: "FormInstances",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_LinkedProjectId",
                table: "FormInstances",
                column: "LinkedProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_PairId",
                table: "FormInstances",
                column: "PairId");

            migrationBuilder.CreateIndex(
                name: "IX_FormInstances_ProjectId",
                table: "FormInstances",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplateBlocks_FormTemplateId",
                table: "FormTemplateBlocks",
                column: "FormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_CreatorId",
                table: "FormTemplates",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_Name",
                table: "FormTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MultichoiceAnswerMultichoiceOption_MultichoiceAnswerId_Multi~",
                table: "MultichoiceAnswerMultichoiceOption",
                columns: new[] { "MultichoiceAnswerId", "MultichoiceOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MultichoiceAnswerMultichoiceOption_MultichoiceOptionId",
                table: "MultichoiceAnswerMultichoiceOption",
                column: "MultichoiceOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiChoiceOption_BlockId",
                table: "MultiChoiceOption",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_OverheadEntries_SessionId",
                table: "OverheadEntries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_OverheadEntries_SprintId",
                table: "OverheadEntries",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_OverheadEntries_UserId",
                table: "OverheadEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeatureFlags_CreatorId",
                table: "ProjectFeatureFlags",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatorId",
                table: "Projects",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_GitlabCredentials_Id_GitlabCredentials_GitlabURL",
                table: "Projects",
                columns: new[] { "GitlabCredentials_Id", "GitlabCredentials_GitlabURL" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserMemberships_UserId",
                table: "ProjectUserMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SinglePerUserFlags_UserId",
                table: "SinglePerUserFlags",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StandUpMeetingAttendance_StandUpMeetingId",
                table: "StandUpMeetingAttendance",
                column: "StandUpMeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_StandUpMeetings_CreatorId",
                table: "StandUpMeetings",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_StandUpMeetings_SprintId",
                table: "StandUpMeetings",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_StandUpMeetings_StartedById",
                table: "StandUpMeetings",
                column: "StartedById");

            migrationBuilder.CreateIndex(
                name: "IX_StandUpMeetingTaskCheckIns_TaskId",
                table: "StandUpMeetingTaskCheckIns",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_StandUpMeetingTaskCheckIns_UserId",
                table: "StandUpMeetingTaskCheckIns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryGroup_BacklogProjectId",
                table: "StoryGroup",
                column: "BacklogProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoryGroup_CreatorId",
                table: "StoryGroup",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryGroup_SprintProjectId",
                table: "StoryGroup",
                column: "SprintProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TaggedWorkInstances_WorklogEntryId",
                table: "TaggedWorkInstances",
                column: "WorklogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TaggedWorkInstances_WorklogTagId",
                table: "TaggedWorkInstances",
                column: "WorklogTagId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStandUpCalendarLinks_ProjectId",
                table: "UserStandUpCalendarLinks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStandUpCalendarLinks_Token",
                table: "UserStandUpCalendarLinks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStories_CreatorId",
                table: "UserStories",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStories_ProjectId",
                table: "UserStories",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStories_StoryGroupId",
                table: "UserStories",
                column: "StoryGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStoryTasks_CreatorId",
                table: "UserStoryTasks",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStoryTasks_UserStoryId",
                table: "UserStoryTasks",
                column: "UserStoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStoryTaskTagJoins_TaskId",
                table: "UserStoryTaskTagJoins",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTaskAssociations_TaskId",
                table: "UserTaskAssociations",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WorklogCommitJoins_EntryId",
                table: "WorklogCommitJoins",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_WorklogEntries_PairUserId",
                table: "WorklogEntries",
                column: "PairUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorklogEntries_TaskId",
                table: "WorklogEntries",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WorklogEntries_UserId",
                table: "WorklogEntries",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnouncementHides");

            migrationBuilder.DropTable(
                name: "ChangelogEntries");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "MultichoiceAnswerMultichoiceOption");

            migrationBuilder.DropTable(
                name: "ProjectFeatureFlags");

            migrationBuilder.DropTable(
                name: "ProjectUserMemberships");

            migrationBuilder.DropTable(
                name: "SinglePerUserFlags");

            migrationBuilder.DropTable(
                name: "StandUpMeetingAttendance");

            migrationBuilder.DropTable(
                name: "UserStandUpCalendarLinks");

            migrationBuilder.DropTable(
                name: "UserStoryTaskTagJoins");

            migrationBuilder.DropTable(
                name: "UserTaskAssociations");

            migrationBuilder.DropTable(
                name: "WorklogCommitJoins");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "AcceptanceCriterias");

            migrationBuilder.DropTable(
                name: "OverheadEntries");

            migrationBuilder.DropTable(
                name: "StandUpMeetingTaskCheckIns");

            migrationBuilder.DropTable(
                name: "TaggedWorkInstances");

            migrationBuilder.DropTable(
                name: "Answers");

            migrationBuilder.DropTable(
                name: "MultiChoiceOption");

            migrationBuilder.DropTable(
                name: "UserStoryTaskTags");

            migrationBuilder.DropTable(
                name: "GitlabCommits");

            migrationBuilder.DropTable(
                name: "OverheadSessions");

            migrationBuilder.DropTable(
                name: "StandUpMeetings");

            migrationBuilder.DropTable(
                name: "WorklogEntries");

            migrationBuilder.DropTable(
                name: "WorklogTags");

            migrationBuilder.DropTable(
                name: "FormInstances");

            migrationBuilder.DropTable(
                name: "FormTemplateBlocks");

            migrationBuilder.DropTable(
                name: "UserStoryTasks");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "UserStories");

            migrationBuilder.DropTable(
                name: "FormTemplates");

            migrationBuilder.DropTable(
                name: "StoryGroup");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
