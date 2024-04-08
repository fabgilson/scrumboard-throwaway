using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Services;

namespace ScrumBoard.Shared.StandUpMeetings;

public partial class StandUpCalendarLinkManagement : BaseProjectScopedComponent
{
    [Inject]
    protected IStandUpCalendarService StandUpCalendarService { get; set; }
    
    private bool _isLoading = true;
    private UserStandUpCalendarLink _calendarLink;
    private string _errorText;
    private string CalendarLinkUrl => _calendarLink is null ? "" : NavigationManager.BaseUri + $"api/StandUpCalendar/GetByToken/{_calendarLink.Token}";

    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await RefreshCalendarLink();
    }
    
    private async Task RefreshCalendarLink()
    {
        _isLoading = true;
        _calendarLink = await StandUpCalendarService.GetStandUpCalendarLinkAsync(Self.Id, Project.Id);
        _isLoading = false;
    }
    
    private async Task RevokeLink()
    {
        try
        {
            _calendarLink = await StandUpCalendarService.ResetTokenForStandUpCalendarLink(Self.Id, Project.Id);
            _errorText = "";
        }
        catch (InvalidOperationException)
        {
            _errorText = "Token was deleted elsewhere, and could not be reset.";
            await RefreshCalendarLink();
        }
    }
    
    private async Task DeleteLink()
    {
        await StandUpCalendarService.DeleteStandUpCalendarLinkAsync(Self.Id, Project.Id);
        _errorText = "";
        await RefreshCalendarLink();
    }
    
    private async Task CreateLink()
    {
        try
        {
            _calendarLink = await StandUpCalendarService.CreateStandUpCalendarLinkAsync(Self.Id, Project.Id);
            _errorText = "";
        }
        catch (InvalidOperationException)
        {
            _errorText = "Could not create a new token as one already exists.";
            await RefreshCalendarLink();
        }
    }
}