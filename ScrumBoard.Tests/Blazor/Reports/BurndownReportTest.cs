using System;
using System.Collections.Generic;
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

public class BurndownReportTest : TestContext
{
    private IRenderedComponent<Burndown> _component;

    private static readonly DateTime Now = DateTime.Now;
    
    private readonly Mock<IJsInteropService> _jsInteropMock = new();
    private readonly Mock<IBurndownService> _burndownService = new();

    private static readonly Sprint _sprintOne = new() { Id = 1, Stage = SprintStage.Closed };
    private static readonly Sprint _sprintTwo = new() { Id = 2, Stage = SprintStage.Started };
    
    private static readonly List<BurndownPoint<double>> _sprintOneBurndownPoints = new()
    {
        new BurndownPoint<double>(Now.AddDays(-10), 10.0, BurndownPointType.NewTask, 1),
        new BurndownPoint<double>(Now.AddDays(-9), 5.0, BurndownPointType.Worklog, 2),
        new BurndownPoint<double>(Now.AddDays(-8), 0.0, BurndownPointType.Worklog, 3),
    };
    private static readonly List<BurndownPoint<double>> _sprintOneBurnupPoints = new()
    {
        new BurndownPoint<double>(Now.AddDays(-10), 0.0, BurndownPointType.Worklog, 3),
        new BurndownPoint<double>(Now.AddDays(-9), 5.0, BurndownPointType.Worklog, 4),
        new BurndownPoint<double>(Now.AddDays(-8), 10.0, BurndownPointType.Worklog, 5),
    };
    
    private static readonly List<BurndownPoint<double>> _sprintTwoBurndownPoints = new()
    {
        new BurndownPoint<double>(Now.AddDays(-1), 10.0, BurndownPointType.NewTask, 11),
        new BurndownPoint<double>(Now, 5.0, BurndownPointType.Worklog, 12),
    };
    private static readonly List<BurndownPoint<double>> _sprintTwoBurnupPoints = new()
    {
        new BurndownPoint<double>(Now.AddDays(-1), 0.0, BurndownPointType.NewTask, 13),
        new BurndownPoint<double>(Now, 5.0, BurndownPointType.Worklog, 14),
    };
    
    public BurndownReportTest()
    {
        _burndownService.Setup(x => x.GetData(_sprintOne, false)).ReturnsAsync(_sprintOneBurndownPoints);
        _burndownService.Setup(x => x.GetData(_sprintOne, true)).ReturnsAsync(_sprintOneBurnupPoints);
        
        _burndownService.Setup(x => x.GetData(_sprintTwo, false)).ReturnsAsync(_sprintTwoBurndownPoints);
        _burndownService.Setup(x => x.GetData(_sprintTwo, true)).ReturnsAsync(_sprintTwoBurnupPoints);
        
        Services.AddScoped(_ => _jsInteropMock.Object);
        Services.AddScoped(_ => _burndownService.Object);
        Services.AddScoped(_ => new Mock<IUsageDataService>().Object); 
        Services.AddScoped(_ => new Mock<ILogger<Burndown>>().Object);
    }

    private void CreateComponent(Sprint sprint)
    {
        _component = RenderComponent<Burndown>(parameters => parameters
            .AddCascadingValue("Self", new User { Id = 13, FirstName = "Jimmy", LastName = "Neutron" })
            .AddCascadingValue("ProjectState", new ProjectState { ProjectId = 100, ProjectRole = ProjectRole.Developer })
            .Add(x => x.Sprint, sprint)
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
    public void FirstRender_SprintIsNull_UpdateChartNotCalled()
    {
        CreateComponent(null);
        VerifyChartUpdated(Times.Never());
    }
    
    [Fact]
    public void FirstRender_SprintIsNotNull_UpdateChartCalled()
    {
        CreateComponent(_sprintOne);
        VerifyChartUpdated(Times.Once());
    }

    [Fact]
    public void SprintParamSet_OldSprintWasNull_UpdateChartCalled()
    {
        CreateComponent(null);
        VerifyChartUpdated(Times.Never());
        UpdateSprintParam(_sprintOne);
        VerifyChartUpdated(Times.Once());
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
    public void SprintParamSetToNull_OldSprintWasNotNullSprint_UpdateChartNotCalled()
    {
        CreateComponent(_sprintOne);
        VerifyChartUpdated(Times.Once());
        UpdateSprintParam(null);
        VerifyChartUpdated(Times.Once());
    }
    
    [Fact]
    public void SprintParamSetToNull_OldSprintWasNull_UpdateChartNotCalled()
    {
        CreateComponent(null);
        VerifyChartUpdated(Times.Never());
        UpdateSprintParam(null);
        VerifyChartUpdated(Times.Never());
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