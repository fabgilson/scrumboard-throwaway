using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.Report;

public partial class MyReflectionCheckIns : BaseProjectScopedComponent
{
    [CascadingParameter(Name = "Sprint")] 
    public Sprint Sprint { get; set; }

    [Inject]
    protected IWeeklyReflectionCheckInService WeeklyReflectionCheckInService { get; set; }
    
    [Inject]
    protected IClock Clock { get; set; }

    private IEnumerable<WeeklyReflectionCheckIn> CheckIns { get; set; }
    
    [Parameter]
    public User SelectedUser { get; set; }
    
    [Inject] 
    protected IUserRepository UserRepository { get; set; }
    
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadCheckInsForUser();
    }
    
    public async Task ChangeUser(User user)
    {
        SelectedUser = await UserRepository.GetByIdAsync(user.Id);
        await LoadCheckInsForUser();
    }

    private WeeklyReflectionCheckIn GetCheckInForWeekAndYear(int isoWeek, int year)
    {
        return (CheckIns ?? []).FirstOrDefault(x => x.IsoWeekNumber == isoWeek && x.Year == year);
    }

    private async Task LoadCheckInsForUser()
    {
        var userToFetchFor = SelectedUser ?? Self;
        
        // Check if current user is permitted to view check-ins for others, if not, ignore selected user value and get
        // user's own check-ins instead. 
        if (RoleInCurrentProject is not ProjectRole.Leader) userToFetchFor = Self;
        
        CheckIns = await WeeklyReflectionCheckInService.GetAllCheckInsForUserForProjectAsync(Project.Id, Sprint?.Id, userToFetchFor.Id);
    }
    
    private IEnumerable<(int IsoWeekNumber, int Year, WeeklyReflectionCheckIn CheckIn)> GetIsoWeeksYearsAndCheckInsForPeriod()
    {
        var start = Sprint?.StartDate.ToDateTime(TimeOnly.MinValue) ?? Project.StartDate.ToDateTime(TimeOnly.MinValue);
        var end = Sprint?.EndDate.ToDateTime(TimeOnly.MaxValue) ?? Project.EndDate.ToDateTime(TimeOnly.MaxValue);
        if (end >= Clock.Now) end = Clock.Now;
        
        var currentDate = start;
        while (currentDate <= GetEndOfWeek(end))
        {
            var isoWeek = ISOWeek.GetWeekOfYear(currentDate);
            var year = ISOWeek.GetYear(currentDate);

            // If the current date is in the first ISO week but in December, it belongs to the next year
            if (isoWeek == 1 && currentDate.Month == 12) year++;
            
            // Similarly, if the current date is in the last ISO week (52 or 53) but in January, it belongs to the previous year
            else if (isoWeek >= 52 && currentDate.Month == 1) year--;

            yield return (isoWeek, year, GetCheckInForWeekAndYear(isoWeek, year));

            currentDate = currentDate.AddDays(7);
        }
    }
    
    private static DateTime GetEndOfWeek(DateTime date)
    {
        var daysToAdd = DayOfWeek.Sunday - date.DayOfWeek;
        if (daysToAdd < 0) daysToAdd += 7;

        return date.AddDays(daysToAdd).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
    }
}