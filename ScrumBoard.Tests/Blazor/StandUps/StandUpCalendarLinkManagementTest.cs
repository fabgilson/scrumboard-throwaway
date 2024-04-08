using System;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ScrumBoard.Models.Entities.Relationships;
using ScrumBoard.Services;
using ScrumBoard.Shared.StandUpMeetings;
using Xunit;

namespace ScrumBoard.Tests.Blazor.StandUps;

public class StandUpCalendarLinkManagementTest : BaseProjectScopedComponentTestContext<StandUpCalendarLinkManagement>
{
    private readonly Mock<IStandUpCalendarService> _standUpCalendarServiceMock;

    private IElement DropdownContainer => ComponentUnderTest.Find(".dropdown-menu");
    private Func<IElement> GetCreateButton => () => ComponentUnderTest.Find("#create-button");
    private Func<IElement> GetResetButton => () => ComponentUnderTest.Find("#reset-button");
    private Func<IElement> GetDeleteButton => () => ComponentUnderTest.Find("#delete-button");
    private Func<IElement> GetErrorLabel => () => ComponentUnderTest.Find(".text-warning");

    private Func<IElement> GetUrlInput => () => ComponentUnderTest.Find("#calendar-link-readonly-input");
    
    public StandUpCalendarLinkManagementTest()
    {
        _standUpCalendarServiceMock = new Mock<IStandUpCalendarService>();
        Services.AddScoped(_ => _standUpCalendarServiceMock.Object);
    }
    
    [Fact]
    public void Rendered_DropdownNotYetOpened_DropdownContainerNotShown()
    {
        CreateComponentUnderTest();
        DropdownContainer.ClassName.Should().NotContain("show");
    }
    
    [Fact]
    public void DropDownOpened_NoLinkExistsForUser_CreateLinkButtonShown()
    {
        CreateComponentUnderTest();
        GetCreateButton().Should().NotBeNull();
        GetResetButton.Should().Throw<ElementNotFoundException>();
        GetDeleteButton.Should().Throw<ElementNotFoundException>();
    }
    
    [Fact]
    public void DropDownOpened_NoLinkExistsForUser_ReadonlyLinkFieldIsEmpty()
    {
        CreateComponentUnderTest();
        GetUrlInput().GetAttribute("value").Should().Be("");
    }

    [Fact]
    public void DropDownOpened_LinkExistsForUser_ResetAndDeleteButtonsShown()
    {
        _standUpCalendarServiceMock
            .Setup(x => x.GetStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id))
            .ReturnsAsync(new UserStandUpCalendarLink { Token = "existing-token"});
            
        CreateComponentUnderTest();
        
        GetCreateButton.Should().Throw<ElementNotFoundException>();
        GetResetButton().Should().NotBeNull();
        GetDeleteButton().Should().NotBeNull();
    }
    
    [Fact]
    public void DropDownOpened_LinkExistsForUser_ReadonlyLinkShown()
    {
        _standUpCalendarServiceMock
            .Setup(x => x.GetStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id))
            .ReturnsAsync(new UserStandUpCalendarLink { Token = "existing-token"});
            
        CreateComponentUnderTest();

        GetUrlInput().GetAttribute("value").Should().EndWith("api/StandUpCalendar/GetByToken/existing-token");
    }
    
    [Fact]
    public void NoLinkExists_CreateLinkButtonClicked_CallsCreateServiceMethod()
    {
        _standUpCalendarServiceMock
            .Setup(x => x.CreateStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id))
            .ReturnsAsync(new UserStandUpCalendarLink { Token = "new-token" })
            .Verifiable();
    
        CreateComponentUnderTest();
        
        GetCreateButton().Click();

        _standUpCalendarServiceMock.Verify(x => x.CreateStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id), Times.Once);
    }

    [Fact]
    public void LinkExists_ResetLinkButtonClicked_CallsResetServiceMethod()
    {
        // Arrange
        _standUpCalendarServiceMock
            .Setup(x => x.GetStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id))
            .ReturnsAsync(new UserStandUpCalendarLink { Token = "existing-token" });

        _standUpCalendarServiceMock
            .Setup(x => x.ResetTokenForStandUpCalendarLink(ActingUser.Id, CurrentProject.Id))
            .ReturnsAsync(new UserStandUpCalendarLink { Token = "new-token" })
            .Verifiable();

        CreateComponentUnderTest();
        
        GetResetButton().Click();

        _standUpCalendarServiceMock.Verify(x => x.ResetTokenForStandUpCalendarLink(ActingUser.Id, CurrentProject.Id), Times.Once);
    }
    
    [Fact]
    public void LinkExists_DeleteLinkButtonClicked_CallsDeleteServiceMethod()
    {
        // Arrange
        _standUpCalendarServiceMock
            .Setup(x => x.GetStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id))
            .ReturnsAsync(new UserStandUpCalendarLink { Token = "existing-token" });

        _standUpCalendarServiceMock
            .Setup(x => x.DeleteStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id))
            .Verifiable();

        CreateComponentUnderTest();
        
        GetDeleteButton().Click();
        
        _standUpCalendarServiceMock.Verify(x => x.DeleteStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id), Times.Once);
    }
    
    [Fact]
    public void RevokeLink_WhenTokenDeletedElsewhere_ShowsErrorMessage()
    {
        _standUpCalendarServiceMock
            .Setup(x => x.GetStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id))
            .ReturnsAsync(new UserStandUpCalendarLink { Token = "existing-token"});
        
        CreateComponentUnderTest();
        GetErrorLabel.Should().Throw<ElementNotFoundException>();

        _standUpCalendarServiceMock
            .Setup(x => x.ResetTokenForStandUpCalendarLink(ActingUser.Id, CurrentProject.Id))
            .ThrowsAsync(new InvalidOperationException());

        GetResetButton().Click();
        GetErrorLabel().TextContent.Should().Contain("Token was deleted elsewhere, and could not be reset.");
    }

    [Fact]
    public void CreateLink_WhenTokenExists_ShowsErrorMessage()
    {
        _standUpCalendarServiceMock
            .Setup(x => x.GetStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id))
            .ReturnsAsync((UserStandUpCalendarLink)null);
        
        CreateComponentUnderTest();
        GetErrorLabel.Should().Throw<ElementNotFoundException>();

        _standUpCalendarServiceMock
            .Setup(x => x.CreateStandUpCalendarLinkAsync(ActingUser.Id, CurrentProject.Id))
            .ThrowsAsync(new InvalidOperationException());

        GetCreateButton().Click();
        GetErrorLabel().TextContent.Should().Contain("Could not create a new token as one already exists.");
    }
}