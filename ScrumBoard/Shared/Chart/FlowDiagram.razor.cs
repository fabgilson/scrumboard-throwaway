using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Messages;
using ScrumBoard.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ScrumBoard.Shared.Widgets;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Models.Entities.UsageData;

namespace ScrumBoard.Shared.Chart;

public partial class FlowDiagram : IComponent, IDisposable
{
    private static readonly IReadOnlyDictionary<Stage, string> StageColours = new Dictionary<Stage, string>()
    {
        [Stage.Todo]        = "rgb(248, 249, 250)",
        [Stage.InProgress]  = "rgb(13, 110, 253)",
        [Stage.UnderReview] = "rgb(255, 193, 7)",
        [Stage.Done]        = "rgb(25, 135, 84)",
        [Stage.Deferred]    = "rgb(220, 53, 69)",
    };

    [CascadingParameter(Name = "Self")]
    public User Self { get; set; }

    [Inject]
    protected IJsInteropService JsInteropService { get; set; }

    [Inject]
    protected IBurndownService BurndownService { get; set; }
        
    [Inject]
    protected ILogger<FlowDiagram> Logger { get; set; }

    [Parameter]
    public Sprint Sprint { get; set; }
    private long? _oldSprintId = -1;
        
    [Parameter]
    public Project Project { get; set; }

    ///<summary>
    /// (Messages displayed, number of additional messages not displayed)
    ///</summary>
    private (List<IMessage>, int)? _tooltipContent;
    
    private bool _shouldRender;

    private static readonly int _maxTooltipMessages = 10;

    private DateTime Start => Sprint?.TimeStarted ?? Project.StartDate.ToDateTime(TimeOnly.MinValue);

    private DateTime End {
        get
        {
            var defaultEnd = (Sprint == null ? Project.EndDate : Sprint.EndDate).ToDateTime(TimeOnly.MaxValue);
                
            var singleLine = _data.First().Value;
            if (!singleLine.Any()) return defaultEnd;
                
            var lastPoint = singleLine.Last().Moment;
            return lastPoint.CompareTo(defaultEnd) > 0 ? lastPoint : defaultEnd;
        }
    } 

    private ElementReference _canvasReference;

    private ElementReference _tooltipTarget;

    private string _tooltipTargetStyle => $"position: absolute; left: {_tooltipX}px; top: {_tooltipY}px";

    private double _tooltipX;
    private double _tooltipY;

    private Dictionary<Stage, List<BurndownPoint<double>>> _data;

    private DotNetObjectReference<FlowDiagram> _objectReference;

    private bool HaveParametersChanged()
    {
        // If is first load
        if (_oldSprintId == -1) return true;
        
        // If selection has changed from some sprint to whole project
        if (Sprint is null && _oldSprintId is not null && _oldSprintId != -1) return true;
        
        // If selection has changed from whole project to some sprint
        if (Sprint is not null && Sprint.Id != _oldSprintId) return true;
        
        return false;
    }
    
    protected override async Task OnParametersSetAsync() {
        await base.OnParametersSetAsync();
        if (!HaveParametersChanged()) return;
        _oldSprintId = Sprint?.Id;
        
        _shouldRender = false;
        if (Sprint != null)
        {
            _data = await BurndownService.GetFlowData(Sprint);
        }
        else
        {
            _data = await BurndownService.GetFlowData(Project);
        }
        _shouldRender = true;
    }
        
    protected override async Task OnAfterRenderAsync(bool firstRender) 
    {
        if (firstRender) _objectReference = DotNetObjectReference.Create(this);
        if(!_shouldRender) return;
        
        try
        {
            await UpdateChart();
        }
        catch (ObjectDisposedException)
        {
            Logger.LogDebug("User navigated away before BurnDown was finished loading");
        }
        _shouldRender = false;
    }

    /// <summary>
    /// Creates an anonymous type with all the options that need to be sent 
    /// to Chart.js. 
    /// Configures chart and tooltips using the JS interop service.
    /// </summary>
    /// <returns>Task to be completed</returns>     
    private async Task UpdateChart()
    {
        var config = new
        {
            Type = "line",
            Options = new
            {
                Interaction = new {
                    Mode = "x",
                },
                Scales = new {
                    X = new {
                        Type = "time",
                        Display = true,
                        Title = new {
                            Display = true,
                            Text = "Date",
                        },
                        Time = new {
                            Unit = "day",
                        },
                        Min = Start,
                        Max = End,
                    },
                    Y = new {
                        Stacked = true,
                        Title = new {
                            Display = true,
                            Text = "Estimated Hours",
                        },
                        Min = 0,
                    },
                },
                Plugins = new {
                    Legend = new {
                        Display = true,
                    },
                    Title = new {
                        Display = false,
                        Text = "Flow Diagram",
                    },
                    Tooltip = new {
                        Enabled = false,
                        Position = "nearest",
                    },
                }
            },
            Data = new
            {
                Datasets = _data
                    .OrderByDescending(x => x.Key)
                    .Select(entry => new
                    {
                        Data = entry.Value,
                        Stepped = true,
                        BackgroundColor = StageColours[entry.Key],
                        PointBorderWidth = 1.5,
                        PointRadius = 0,
                        PointHitRadius = 10,
                        BorderWidth = entry.Key == Stage.Todo ? 1 : 0,
                        Fill = true,
                        Label = StageDetails.StageDescriptions[entry.Key],
                    })
            }
        };

        await JsInteropService.ChartSetup(_canvasReference, config);
        await JsInteropService.UseExternalTooltip(_canvasReference, _objectReference);
    }

    /// <summary>
    /// Is called from javascript to display a tooltip whenever a point on the flow diagram chart is hovered over. 
    /// </summary>
    /// <param name="points">List of burndown points</param>
    /// <param name="x">x co-ordinate of where the tooltip is to be displayed</param>
    /// <param name="y">y co-ordinate of where the tooltip is to be displayed</param>
    /// <returns>Task to be completed</returns>
    [JSInvokable("showTooltip")]       
    public async Task ShowTooltip(List<BurndownPoint<double>> points, double x, double y)
    {
        points = points
            .Where(point => point.Type != BurndownPointType.None)
            .DistinctBy(point => (point.Moment, point.Type, point.Id))
            .ToList();
        if (!points.Any()) {
            _tooltipContent = null;
            StateHasChanged();
            return;
        }


        int excessMessages = 0;
        if (points.Count() > _maxTooltipMessages) {
            excessMessages = points.Count() - _maxTooltipMessages + 1;
            points.RemoveRange(_maxTooltipMessages - 1, excessMessages);
        }
        var messages = new List<IMessage>();
        foreach (var point in points) 
            messages.Add(await BurndownService.GenerateMessage(point));
            
        _tooltipContent = (messages, excessMessages);
        _tooltipX = x;
        _tooltipY = y;
        StateHasChanged();
    }

    /// <summary>
    /// Called from javascript to hide the tooltip whenever the mouse moves away from a point 
    /// in the burndown chart.
    /// </summary>
    [JSInvokable("hideTooltip")]       
    public void HideTooltip()
    {
        _tooltipContent = null;
        StateHasChanged();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (_objectReference != null) { // May not have rendered at all
            _objectReference.Dispose();
        }
    }
}