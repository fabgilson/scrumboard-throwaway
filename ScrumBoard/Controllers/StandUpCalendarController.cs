using System.Threading.Tasks;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ScrumBoard.Services;

namespace ScrumBoard.Controllers;

[AllowAnonymous]
[ApiController]
public class StandUpCalendarController : ControllerBase
{
    private readonly IStandUpCalendarService _calendarService;
    private readonly IProjectService _projectService;
    private readonly ILogger<StandUpCalendarController> _logger;

    public StandUpCalendarController(IStandUpCalendarService calendarService, IProjectService projectService, ILogger<StandUpCalendarController> logger)
    {
        _calendarService = calendarService;
        _projectService = projectService;
        _logger = logger;
    }

    [Route("api/[controller]/[action]/{token}")]
    public async Task<IActionResult> GetByToken([FromRoute] string token)
    {
        _logger.LogInformation("Calendar requested for token: {token}", token);
        var link = await _calendarService.GetStandUpCalendarLinkByTokenAsync(token);
        if (link is null) return NotFound();

        var userMembershipInProject = await _projectService.GetUserMembershipInProjectAsync(link.ProjectId, link.UserId);
        if (userMembershipInProject is null) return BadRequest();

        var calendar = await _calendarService.GetCalendarForProjectAsync(link.ProjectId);
        
        var serializer = new CalendarSerializer(new SerializationContext());
        var serializedCalendar = serializer.SerializeToString(calendar);

        return Content(serializedCalendar, "text/calendar");
    }
}