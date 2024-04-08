using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Messages;
using ScrumBoard.Services;

namespace ScrumBoard.Shared.Chart;

public partial class Burndown : IDisposable
{
    [CascadingParameter(Name = "Self")]
    public User Self { get; set; }

    [Inject]
    protected IJsInteropService JsInteropService { get; set; }

    [Inject]
    protected IBurndownService BurndownService { get; set; }
        
    [Inject]
    protected ILogger<Burndown> Logger { get; set; }

    [Parameter]
    public Sprint Sprint { get; set; }
    private long? _oldSprintId = -1;

    ///<summary>
    /// (Messages displayed, number of additional messages not displayed)
    ///</summary>
    private (List<IMessage>, int)? _tooltipContent;
    
    private static readonly int _maxTooltipMessages = 10;

    private DateTime _start;
    private DateTime _end;

    private ElementReference _canvasReference;
    private ElementReference _tooltipTarget;

    private string TooltipTargetStyle => $"position: absolute; left: {_tooltipX}px; top: {_tooltipY}px";

    private double _tooltipX;
    private double _tooltipY;

    private List<BurndownPoint<double>> _burndownPoints;
    private List<BurndownPoint<double>> _burnupPoints;

    private DotNetObjectReference<Burndown> _objectReference;

    private string _burndownColour = "#dc3545"; // --bs-red'
    private string _burnupColour = "#198754"; // --bs-success'
    private string _idealLineColour = "#0000001a";

    private bool _shouldRenderChart;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
            
        // If no parameters have changed, don't load data again
        if (Sprint is null || (_oldSprintId is not null && _oldSprintId == Sprint.Id)) return;
        _oldSprintId = Sprint.Id;

        _shouldRenderChart = false;
        _burndownPoints = await BurndownService.GetData(Sprint, false);
        _burnupPoints = await BurndownService.GetData(Sprint, true);

        _start = _burndownPoints.First().Moment;
        _end = Sprint.EndDate.ToDateTime(TimeOnly.MaxValue);
            
        _shouldRenderChart = true;
    }
        
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender) _objectReference = DotNetObjectReference.Create(this);
        if (!_shouldRenderChart) return;
            
        try
        {
            await UpdateChart();
        }
        catch (ObjectDisposedException)
        {
            Logger.LogDebug("User navigated away before BurnDown was finished loading");
        }

        _shouldRenderChart = false;
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
                        Min = _start,
                        Max = _end,
                    },
                    Y = new {
                        Title = new {
                            Display = true,
                            Text = "Hours",
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
                        Text = "Burndown",
                    },
                    Tooltip = new {
                        Enabled = false,
                        Position = "nearest",
                    },
                }
            },
            Data = new
            {
                Datasets = new object[]
                {
                    new {
                        Label = "Time Remaining",
                        Data = _burndownPoints,
                        Stepped = true,
                        BorderColor = _burndownColour, 
                        BackgroundColor = _burndownColour,
                        PointBorderWidth = 1.5,
                        PointHitRadius = 10,
                    },
                    new {
                        Label = "Time Spent",
                        Data = _burnupPoints,
                        Stepped = true,
                        BorderColor = _burnupColour, 
                        BackgroundColor = _burnupColour,
                        PointBorderWidth = 1.5,
                        PointHitRadius = 10,
                    },
                    new {
                        Label = "Ideal Line",
                        Data = new List<BurndownPoint<double>> {
                            BurndownPoint<double>.Initial(_start, _burndownPoints.Max(p => p.Value)),
                            BurndownPoint<double>.Initial(_end, 0),
                        },
                        PointRadius = 0,
                        BorderColor = _idealLineColour,
                        BackgroundColor = _idealLineColour,
                        PointHitRadius = 0,
                    }                        
                },
            }
        };
            
        await JsInteropService.ChartSetup(_canvasReference, config);
        await JsInteropService.UseExternalTooltip(_canvasReference, _objectReference);
    }

    /// <summary>
    /// Is called from javascript to display a tooltip whenever a point on the burndown chart is hovered over. 
    /// </summary>
    /// <param name="points">List of burndown points</param>
    /// <param name="x">x co-ordinate of where the tooltip is to be displayed</param>
    /// <param name="y">y co-ordinate of where the tooltip is to be displayed</param>
    /// <returns>Task to be completed</returns>
    [JSInvokable("showTooltip")]       
    public async Task ShowTooltip(List<BurndownPoint<double>> points, double x, double y)
    {
        points = points.Where(point => point.Type != BurndownPointType.None).DistinctBy(point => new { point.Moment, point.Type, point.Id } ).ToList();
        if (!points.Any()) {
            _tooltipContent = null;
            StateHasChanged();
            return;
        }

        var excessMessages = 0;
        if (points.Count > _maxTooltipMessages) {
            excessMessages = points.Count - _maxTooltipMessages + 1;
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
        _objectReference?.Dispose();
    }
}