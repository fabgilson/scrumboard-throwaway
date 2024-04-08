using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.Widgets;

public partial class LiveUpdateNotifier<TEntity> : BaseProjectScopedComponent where TEntity : IId
{
    private enum LiveUpdateState
    {
        None = 0,
        EditingStarted = 1,
        EditingStopped = 2,
        EntityUpdated = 3,
    }
    
    [Parameter]
    public long EntityId { get; set; }
    
    [Inject]
    protected IUserRepository UserRepository { get; set; }
    
    [Inject]
    protected IClock Clock { get; set; }

    private User _lastEditingUser;
    private LiveUpdateState _lastState = LiveUpdateState.None;
    private DateTime? _lastEditOccurred;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);

        RegisterListenerForUpdateBegun<TEntity>(
            EntityId,
            async editingUserId =>
            {
                if(editingUserId == Self.Id) return;
                _lastEditingUser = await UserRepository.GetByIdAsync(editingUserId);
                _lastState = LiveUpdateState.EditingStarted;
                StateHasChanged();
            }
        );

        RegisterListenerForUpdateEnded<TEntity>(
            EntityId,
            editingUserId =>
            {
                if(editingUserId == Self.Id || _lastState is LiveUpdateState.EntityUpdated) return;
                _lastEditingUser = null;
                _lastState = LiveUpdateState.EditingStopped;
                StateHasChanged();
            }
        );
        
        RegisterNewLiveEntityUpdateHandler<TEntity>(
            EntityId,
            async (_, editingUserId) =>
            {
                if(editingUserId == Self.Id) return;
                _lastEditingUser = await UserRepository.GetByIdAsync(editingUserId);
                _lastState = LiveUpdateState.EntityUpdated;
                _lastEditOccurred = Clock.Now;
                StateHasChanged();
            }
        );
    }
    
    private static string LastEditedDateTimeFormatFunc(DateTime now, DateTime target)
    {
        var timeSinceChange = target.Subtract(now).Duration();
        
        if (timeSinceChange < TimeSpan.FromSeconds(10)) 
            return "just now";
        
        var timeSinceChangeString = DurationUtils.DurationStringFrom(
            timeSinceChange,
            DurationFormatOptions.AlwaysShowAsPositiveValue
            | DurationFormatOptions.TakeHighestUnitOnly
            | DurationFormatOptions.UseDaysAsLargestUnit
        );
        return $"{timeSinceChangeString} ago";
    }
}