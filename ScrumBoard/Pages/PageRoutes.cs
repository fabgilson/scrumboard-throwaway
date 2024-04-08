using ScrumBoard.Models.Statistics;
using Routing = SharedLensResources.Blazor.Util.PageRoutingUtils;

namespace ScrumBoard.Pages;

public static class PageRoutes
{
    public const string Root = "/";
    public static string ToRoot() => Routing.GenerateRelativeUrlWithParams(Root);
    
    public const string AdminDashboard = "/admin-dashboard";
    public const string StudentGuideMediaFolder = "/images/student-guide-media/";

    public static string ToAdminDashboard() => Routing.GenerateRelativeUrlWithParams(AdminDashboard);
    
    public const string AdminStandUpSchedule = "/admin-dashboard/stand-up-schedule";
    public static string ToAdminStandUpSchedule() => Routing.GenerateRelativeUrlWithParams(AdminStandUpSchedule);
    
    public const string AdminFormManagement = "/admin-dashboard/form-management";
    public static string ToAdminFormManagement() => Routing.GenerateRelativeUrlWithParams(AdminFormManagement);
    
    public const string CreateProject = "/project/create";
    public static string ToCreateProject() => Routing.GenerateRelativeUrlWithParams(CreateProject);

    public const string ProjectHome = "/project/{ProjectId:long}";
    public static string ToProjectHome(long projectId) => Routing.GenerateRelativeUrlWithParams(ProjectHome, ("ProjectId", projectId));

    public const string ProjectChangeLog = $"{ProjectHome}/changelog";
    public static string ToProjectChangeLog(long projectId) => Routing.GenerateRelativeUrlWithParams(ProjectChangeLog, ("ProjectId", projectId));

    public const string ProjectBacklog = $"{ProjectHome}/backlog";
    public static string ToProjectBacklog(long projectId) => Routing.GenerateRelativeUrlWithParams(ProjectBacklog, ("ProjectId", projectId));
    
    public const string ProjectSprintBoard = $"{ProjectHome}/board";
    public static string ToProjectSprintBoard(long projectId) => Routing.GenerateRelativeUrlWithParams(ProjectSprintBoard, ("ProjectId", projectId));

    private const string ProjectReportFallback = $"{ProjectHome}/report";
    public const string ProjectReport = $"{ProjectReportFallback}/{{ReportTypeParam:int?}}";
    public static string ToProjectReport(long projectId, ReportType? reportType = null) =>
        reportType is null
            ? Routing.GenerateRelativeUrlWithParams(ProjectReportFallback, ("ProjectId", projectId))
            : Routing.GenerateRelativeUrlWithParams(ProjectReport, ("ProjectId", projectId), ("ReportTypeParam", (int)reportType));

    public const string ProjectCeremonies = $"{ProjectHome}/formal-events";
    public static string ToProjectCeremonies(long projectId) => Routing.GenerateRelativeUrlWithParams(ProjectCeremonies, ("ProjectId", projectId));

    public const string ProjectReview = $"{ProjectHome}/review";
    public static string ToProjectReview(long projectId) => Routing.GenerateRelativeUrlWithParams(ProjectReview, ("ProjectId", projectId));

    public const string StandUpSchedule = $"{ProjectHome}/daily-scrum-schedule";
    public static string ToStandUpSchedule(long projectId) => Routing.GenerateRelativeUrlWithParams(StandUpSchedule, ("ProjectId", projectId));
    
    public const string UpcomingStandUp = $"{ProjectHome}/upcoming-daily-scrum";
    public static string ToUpcomingStandUp(long projectId) => Routing.GenerateRelativeUrlWithParams(UpcomingStandUp, ("ProjectId", projectId));

    public const string StudentGuide = "/student-guide/{PageName?}";
    public static string ToStudentGuidePage(string pageName=null, string targetedAnchor=null) => 
        Routing.GenerateRelativeUrlWithParams(StudentGuide, targetedAnchor, ("PageName", pageName ?? ""));

    public const string FillForms = $"{ProjectHome}/fill-forms";

    public static string ToFillForms(long projectId) =>
        Routing.GenerateRelativeUrlWithParams(FillForms, ("ProjectId", projectId));

    public const string FillSingleForm = $"{FillForms}/{{FormId:long}}";

    public static string ToFillSingleForm(long projectId, long formId) =>
        Routing.GenerateRelativeUrlWithParams(FillSingleForm, ("ProjectId", projectId), ("FormId", formId));

    public const string ViewAssignedProjectForms = $"{ProjectHome}/assigned-forms";
    public static object ToViewAssignedProjectForms(long projectId) =>
        Routing.GenerateRelativeUrlWithParams(ViewAssignedProjectForms, ("ProjectId", projectId));
    
    public const string ViewFormResponses = $"{ProjectHome}/view-form-responses/{{AssignmentId:long}}";
    public static string ToViewFormResponses(long assignmentId, long projectId) => 
        Routing.GenerateRelativeUrlWithParams(ViewFormResponses, ("AssignmentId", assignmentId), ("ProjectId", projectId));
    
    public const string WeeklyReflectionCheckIn = $"{ProjectHome}/weekly-reflection-check-in";
    public static string ToWeeklyReflectionCheckIn(long projectId) =>
        Routing.GenerateRelativeUrlWithParams(WeeklyReflectionCheckIn, ("ProjectId", projectId));
}