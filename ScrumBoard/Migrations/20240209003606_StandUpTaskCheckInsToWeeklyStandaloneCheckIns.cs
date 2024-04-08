using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrumBoard.Migrations
{
    public partial class StandUpTaskCheckInsToWeeklyStandaloneCheckIns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChangelogEntries_StandUpMeetingTaskCheckIns_CheckInStandUpMe~",
                table: "ChangelogEntries");

            migrationBuilder.DropTable(
                name: "StandUpMeetingTaskCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_ChangelogEntries_CheckInStandUpMeetingId_CheckInTaskId_Check~",
                table: "ChangelogEntries");

            migrationBuilder.DropColumn(
                name: "CheckInStandUpMeetingId",
                table: "ChangelogEntries");

            migrationBuilder.DropColumn(
                name: "CheckInTaskId",
                table: "ChangelogEntries");

            migrationBuilder.DropColumn(
                name: "CheckInUserId",
                table: "ChangelogEntries");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "UserStoryTasks",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "UserStories",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "StoryGroup",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "Projects",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "OverheadEntries",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "FormTemplates",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "AcceptanceCriterias",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.CreateTable(
                name: "WeeklyReflectionCheckIns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "varchar(10000)", maxLength: 10000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsoWeekNumber = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletionStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyReflectionCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyReflectionCheckIns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeeklyReflectionCheckIns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TaskCheckIns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WeeklyReflectionCheckInId = table.Column<long>(type: "bigint", nullable: false),
                    TaskId = table.Column<long>(type: "bigint", nullable: false),
                    CheckInTaskDifficulty = table.Column<int>(type: "int", nullable: false),
                    CheckInTaskStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCheckIns_UserStoryTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "UserStoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskCheckIns_WeeklyReflectionCheckIns_WeeklyReflectionCheckI~",
                        column: x => x.WeeklyReflectionCheckInId,
                        principalTable: "WeeklyReflectionCheckIns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCheckIns_TaskId",
                table: "TaskCheckIns",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCheckIns_WeeklyReflectionCheckInId",
                table: "TaskCheckIns",
                column: "WeeklyReflectionCheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReflectionCheckIns_ProjectId",
                table: "WeeklyReflectionCheckIns",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyReflectionCheckIns_UserId",
                table: "WeeklyReflectionCheckIns",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskCheckIns");

            migrationBuilder.DropTable(
                name: "WeeklyReflectionCheckIns");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "UserStoryTasks",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "UserStories",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "StoryGroup",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "Projects",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "OverheadEntries",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "FormTemplates",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AddColumn<long>(
                name: "CheckInStandUpMeetingId",
                table: "ChangelogEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CheckInTaskId",
                table: "ChangelogEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CheckInUserId",
                table: "ChangelogEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "AcceptanceCriterias",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.CreateTable(
                name: "StandUpMeetingTaskCheckIns",
                columns: table => new
                {
                    StandUpMeetingId = table.Column<long>(type: "bigint", nullable: false),
                    TaskId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CompletionStatus = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_ChangelogEntries_CheckInStandUpMeetingId_CheckInTaskId_Check~",
                table: "ChangelogEntries",
                columns: new[] { "CheckInStandUpMeetingId", "CheckInTaskId", "CheckInUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_StandUpMeetingTaskCheckIns_TaskId",
                table: "StandUpMeetingTaskCheckIns",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_StandUpMeetingTaskCheckIns_UserId",
                table: "StandUpMeetingTaskCheckIns",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChangelogEntries_StandUpMeetingTaskCheckIns_CheckInStandUpMe~",
                table: "ChangelogEntries",
                columns: new[] { "CheckInStandUpMeetingId", "CheckInTaskId", "CheckInUserId" },
                principalTable: "StandUpMeetingTaskCheckIns",
                principalColumns: new[] { "StandUpMeetingId", "TaskId", "UserId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
