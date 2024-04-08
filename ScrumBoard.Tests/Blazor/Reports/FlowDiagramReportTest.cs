using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ScrumBoard.Models;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Shared;
using ScrumBoard.Shared.Chart;
using Xunit;

namespace ScrumBoard.Tests.Blazor.Reports;

public class FlowDiagramReportTest : TestContext
{
    private IRenderedComponent<FlowDiagram> _component;

    private static readonly DateTime Now = DateTime.Now;
    
    private readonly Mock<IJsInteropService> _jsInteropMock = new();
    private readonly Mock<IBurndownService> _burndownService = new();

    private static readonly Project _project = new() { Id = 100, StartDate = DateOnly.FromDateTime(Now.AddDays(-15))};
    
    private static readonly Sprint _sprintOne = new() { Id = 1, Stage = SprintStage.Closed, TimeStarted = Now.AddDays(-10)};
    private static readonly Sprint _sprintTwo = new() { Id = 2, Stage = SprintStage.Started, TimeStarted = Now.AddDays(-1)};
    
    private static readonly Dictionary<Stage, List<BurndownPoint<double>>> _sprintOneFlowPoints = new()
    {
        { Stage.Todo, new List<BurndownPoint<double>> {new (Now.AddDays(-10), 10.0, BurndownPointType.NewTask, 1)}},
        { Stage.InProgress, new List<BurndownPoint<double>> { new(Now.AddDays(-9), 10.0, BurndownPointType.StageChange, 2) }},
        { Stage.UnderReview, new List<BurndownPoint<double>> { new(Now.AddDays(-8), 10.0, BurndownPointType.StageChange, 3) }},
        { Stage.Done, new List<BurndownPoint<double>> { new(Now.AddDays(-9), 10.0, BurndownPointType.StageChange, 4) }},
    };
    
    private static readonly Dictionary<Stage, List<BurndownPoint<double>>> _sprintTwoFlowPoints = new()
    {
        { Stage.Todo, new List<BurndownPoint<double>> {new (Now.AddDays(-1), 10.0, BurndownPointType.NewTask, 11)}},
        { Stage.InProgress, new List<BurndownPoint<double>>{ new(Now, 10.0, BurndownPointType.StageChange, 12)}},
    };
    
    private static readonly Dictionary<Stage, List<BurndownPoint<double>>> _joinedFlowPoints = _sprintOneFlowPoints
        .Concat(_sprintTwoFlowPoints)
        .GroupBy(x => x.Key)
        .ToDictionary(g => g.Key, g => g.SelectMany(x => x.Value).ToList());
    
    public FlowDiagramReportTest()
    {
        _burndownService.Setup(x => x.GetFlowData(_sprintOne)).ReturnsAsync(_sprintOneFlowPoints);
        _burndownService.Setup(x => x.GetFlowData(_sprintTwo)).ReturnsAsync(_sprintTwoFlowPoints);
        _burndownService.Setup(x => x.GetFlowData(_project)).ReturnsAsync(_joinedFlowPoints);

        Services.AddScoped(_ => _jsInteropMock.Object);
        Services.AddScoped(_ => _burndownService.Object);
        Services.AddScoped(_ => new Mock<IUsageDataService>().Object); 
        Services.AddScoped(_ => new Mock<ILogger<Burndown>>().Object);
    }

    private void CreateComponent(Sprint sprint)
    {
        _component = RenderComponent<FlowDiagram>(parameters => parameters
            .AddCascadingValue("Self", new User { Id = 13, FirstName = "Jimmy", LastName = "Neutron" })
            .AddCascadingValue("ProjectState", new ProjectState { ProjectId = _project.Id, ProjectRole = ProjectRole.Developer })
            .Add(x => x.Sprint, sprint)
            .Add(x => x.Project, _project)
        );
    }

    private void VerifyChartUpdated(Times times)
    {
        _jsInteropMock.Verify(x => 
            x.ChartSetup(It.IsAny<ElementReference>(), It.IsAny<object>()), 
            times
        );
    }

    private void UpdateSprintParam(Sprint sprint)
    {
        _component.SetParametersAndRender(p => 
            p.Add(x => x.Sprint, sprint));
    }
    
    [Fact]
    public void FirstRender_SprintIsNull_UpdateChartCalled()
    {
        CreateComponent(null);
        VerifyChartUpdated(Times.Once());
    }

    [Fact]
    public void SprintParamSet_OldSprintWasNull_UpdateChartCalled()
    {
        CreateComponent(null);
        VerifyChartUpdated(Times.Once());
        UpdateSprintParam(_sprintOne);
        VerifyChartUpdated(Times.Exactly(2));
    }
    
    [Fact]
    public void SprintParamSet_OldSprintWasDifferentSprint_UpdateChartCalled()
    {
        CreateComponent(_sprintOne);
        VerifyChartUpdated(Times.Once());
        UpdateSprintParam(_sprintTwo);
        VerifyChartUpdated(Times.Exactly(2));
    }
    
    [Fact]
    public void SprintParamSetToNull_OldSprintWasNotNullSprint_UpdateChartCalled()
    {
        CreateComponent(_sprintOne);
        VerifyChartUpdated(Times.Once());
        UpdateSprintParam(null);
        VerifyChartUpdated(Times.Exactly(2));
    }
    
    [Fact]
    public void SprintParamSetToNull_OldSprintWasNull_UpdateChartNotCalled()
    {
        CreateComponent(null);
        VerifyChartUpdated(Times.Once());
        UpdateSprintParam(null);
        VerifyChartUpdated(Times.Once());
    }
    
    [Fact]
    public void SprintParamSet_OldSprintWasSameSprint_UpdateChartNotCalled()
    {
        CreateComponent(_sprintOne);
        VerifyChartUpdated(Times.Once());
        UpdateSprintParam(_sprintOne);
        VerifyChartUpdated(Times.Once());    
    }
}