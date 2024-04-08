using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities.Changelog;
using ScrumBoard.Models.Entities.ReflectionCheckIns;
using ScrumBoard.Services;

namespace ScrumBoard.Shared.ReflectionCheckIns;

public partial class WeeklyReflectionReadonlyDisplay
{
    [Parameter, EditorRequired]
    public WeeklyReflectionCheckIn CheckIn { get; set; }
    
    [Parameter, EditorRequired]
    public int IsoWeek { get; set; }
    
    [Parameter, EditorRequired]
    public int Year { get; set; }
    
    [Parameter]
    public bool EnableChangelog { get; set; }

    [Inject] protected IWeeklyReflectionCheckInService WeeklyReflectionCheckInService { get; set; }

    private bool _showChangelog;
    private ICollection<WeeklyReflectionCheckInChangelogEntry> _changelogEntries = [];
    private bool _showAnythingElse;

    private static DateTime GetFirstDayOfWeek(int isoYear, int isoWeek) => ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday);
    private static DateTime GetLastDayOfWeek(int isoYear, int isoWeek) => ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Sunday);

    private async Task LoadChangelog()
    {
        if(CheckIn is null || CheckIn.Id == default) return;
        _changelogEntries = await WeeklyReflectionCheckInService.GetChangelogsForCheckInAsync(CheckIn.Id);
    }

}