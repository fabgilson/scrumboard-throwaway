using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Services;

namespace ScrumBoard.Shared;

public struct NamedValue
{
    public NamedValue()
    {
        Name = null;
        Value = 0;
    }

    public string Name { get; set; }
    public double Value { get; set; }
}

public struct NamedSeries
{
    public string Name { get; set; }
    public string Color { get; set; }
    public string BorderColor { get; set; }
    public IEnumerable<NamedValue> NamedValues { get; set; }
}

public partial class TwoSeriesBarChart : ComponentBase
{
    [Parameter]
    public IList<NamedSeries> NamedSeries { get; set; }
    private IList<NamedSeries> _previousNamedSeries;

    [Parameter]
    public string SeriesLabel { get; set; }
    
    [Inject]
    protected IJsInteropService JsInteropService { get; set; }
    
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object> AdditionalAttributes { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if(_previousNamedSeries is not null && _previousNamedSeries.Equals(NamedSeries)) return;
        _previousNamedSeries = NamedSeries;
        await UpdateChart();
    }

    private async Task UpdateChart()
    {
        var config = new
        {
            Type = "bar",
            Data = new
            {
                Labels = NamedSeries.First().NamedValues.Select(x => x.Name),
                Datasets = new object[]
                {
                    new {
                        Label = NamedSeries[0].Name,
                        Data = NamedSeries[0].NamedValues.Select(x => x.Value),
                        BorderColor = NamedSeries[0].BorderColor,
                        BorderWidth = 2,
                        BackgroundColor = NamedSeries[0].Color,
                        YAxisID = "y"
                    },
                    new {
                        Label = NamedSeries[1].Name,
                        Data = NamedSeries[1].NamedValues.Select(x => x.Value),
                        BorderColor = NamedSeries[1].BorderColor,
                        BorderWidth = 2,
                        BackgroundColor = NamedSeries[1].Color,
                        YAxisID = "y1"
                    }
                }
            },
            Options = new
            {
                Responsive = true,
                MaintainAspectRatio = false,
                Scales = new
                {
                    Y = new
                    {
                        Title = new
                        {
                            Text = NamedSeries[0].Name,
                            Display = true
                        },
                        Type = "linear",
                        Position = "left",
                        Grid = new
                        {
                            Display = false
                        },
                        Ticks = new
                        {
                            Precision = 0
                        }
                    },
                    Y1 = new
                    {
                        Title = new
                        {
                            Text = NamedSeries[1].Name,
                            Display = true
                        },
                        Type = "linear",
                        Position = "right"
                    }
                }
            }
        };
        
        await JsInteropService.ChartSetup("canvasRef", config);
    }
}