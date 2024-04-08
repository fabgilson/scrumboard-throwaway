using System;
using FluentAssertions;
using ScrumBoard.Models.Statistics;
using ScrumBoard.Pages;
using SharedLensResources.Blazor.Util;
using Xunit;

namespace ScrumBoard.Tests.Unit.Utils;

public class PageRoutesTest
{
    [Theory]
    [InlineData(nameof(PageRoutes.ToAdminDashboard), "./admin-dashboard")]
    [InlineData(nameof(PageRoutes.ToAdminStandUpSchedule), "./admin-dashboard/stand-up-schedule")]
    [InlineData(nameof(PageRoutes.ToCreateProject), "./project/create")]
    private void NavigationTo_WithoutRouteParams_UrlOutputCorrectly(string toRouteMethodName, string expectedOutput)
    {
        var toRouteMethod = typeof(PageRoutes).GetMethod(toRouteMethodName);
        var methodOutput = toRouteMethod!.Invoke(null, null);
        methodOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData(nameof(PageRoutes.ToProjectBacklog), "./project/123/backlog", 123)]
    [InlineData(nameof(PageRoutes.ToProjectCeremonies), "./project/456/formal-events", 456)]
    [InlineData(nameof(PageRoutes.ToProjectHome), "./project/789", 789)]
    [InlineData(nameof(PageRoutes.ToProjectReport), "./project/100/report", 100, null)] // If no report specified
    [InlineData(nameof(PageRoutes.ToProjectReport), "./project/100/report/3", 100, ReportType.WorkLog)] 
    private void NavigationTo_WithRouteParams_UrlOutputCorrectly(string toRouteMethodName, string expectedOutput, params object[] routeParams)
    {
        var toRouteMethod = typeof(PageRoutes).GetMethod(toRouteMethodName);
        var methodOutput = toRouteMethod!.Invoke(null, routeParams);
        methodOutput.Should().Be(expectedOutput);
    }

    [Fact]
    private void GeneratingUrl_InvalidParameterTypeGiven_ArgumentExceptionThrown()
    {
        var action = () => PageRoutingUtils.GenerateRelativeUrlWithParams(PageRoutes.ProjectHome, ("ProjectId", 1.0f));
        action.Should().Throw<ArgumentException>();
    }

    public static readonly TheoryData<string, ValueTuple<string, object>[]> InvalidRoutesUrlParameterTheory = new()
    {
        {PageRoutes.AdminDashboard, new []{ ("ProjectId", (object)1) }}, // Expects 0 parameters
        {PageRoutes.ProjectHome, Array.Empty<(string, object)>() }, // Expects 1 parameter
        {PageRoutes.ProjectHome, new []{ ("ProjectId", (object)1), ("OtherParam", 1) } }, // Expects 1 parameter
        {PageRoutes.ProjectReport, new []{ ("ProjectId", (object)1), ("OtherParam", 1), ("ReportType", 1) } }, // Expects 2 parameters
    };

    [Theory]
    [MemberData(nameof(InvalidRoutesUrlParameterTheory))]
    private void GeneratingUrl_WrongNumberOfParametersGiven_InvalidOperationExceptionThrown(string routeTemplate, params (string, object)[] routeParams)
    {
        var action = () => PageRoutingUtils.GenerateRelativeUrlWithParams(routeTemplate, routeParams);
        action.Should().Throw<InvalidOperationException>();
    }
}