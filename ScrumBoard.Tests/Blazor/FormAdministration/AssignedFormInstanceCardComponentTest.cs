using System;
using System.Collections.Generic;
using System.Globalization;
using Bunit;
using FluentAssertions;
using FluentAssertions.Execution;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Shared.Widgets;
using Xunit;

namespace ScrumBoard.Tests.Blazor.FormAdministration;

public class AssignedFormInstanceCardComponentTest : TestContext
{
    private IRenderedFragment _component;
    private static readonly User RecipientUser = new User() { Id = 1 };
    private static readonly User NonRecipientUser = new User() { Id = 2 };
        
    private static readonly Project Project1 = new Project
    {
        Id = 1, 
        Name = "Team 600",
        MemberAssociations = new List<ProjectUserMembership>
        {
            new(){UserId = RecipientUser.Id, User = RecipientUser},
            new(){UserId = NonRecipientUser.Id, User = NonRecipientUser}
        }
    };
    
    private static readonly FormInstance Instance1 = new UserFormInstance
    {
        Project = Project1,
        ProjectId = Project1.Id,
        Assignee = RecipientUser
    };
    
    private static readonly Assignment Assignment1 = new Assignment
    {
        Name="Sprint 1 Peer Feedback",
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(3),
        RunNumber = 0,
        Instances = new List<FormInstance> { Instance1 }
    };

    public AssignedFormInstanceCardComponentTest()
    {
        Instance1.Assignment = Assignment1;
    }

    private void CreateComponent()
    {
        _component = RenderComponent<AssignedFormInstanceCard>(parameters => 
            parameters
                .Add(x => x.Assignment, Assignment1));
    }
    
    [Fact]
    public void AssignedFormInstanceCard_OnRender_DisplaysRunInfoCorrectly()
    {
        CreateComponent();

        using (new AssertionScope())
        {
            _component.Find(".run-number").InnerHtml.Should().Contain(Assignment1.RunNumber.ToString());
            _component.Find(".instance-name").InnerHtml.Should().Contain(Assignment1.Name);
            _component.Find(".start-date").InnerHtml.Should().Contain(Assignment1.StartDate.ToString(CultureInfo.CurrentCulture));
            _component.Find(".end-date").InnerHtml.Should().Contain(Assignment1.EndDate.ToString(CultureInfo.CurrentCulture));
        }
    }

    [Fact]
    public void AssignedFormInstanceCard_ClickDownArrow_ShowsProjects()
    {
        CreateComponent();
        _component.Find(".bi-chevron-down").Click();
        _component.Find(".project-name").InnerHtml.Should().Contain(Project1.Name);
    }
    
    [Fact]
    public void AssignedFormInstanceCard_ClickDownArrow_ShowsAssignedUsers()
    {
        CreateComponent();
        _component.Find(".bi-chevron-down").Click();

        using (new AssertionScope())
        {
            // The second user is in the project but was not in the list of recipients, so shouldn't be listed here
            _component.FindComponents<UserAvatar>().Count.Should().Be(1);
            _component.Find("img").Attributes.GetNamedItem("alt")!.Value.Should()
                .Be($"{RecipientUser.FirstName} {RecipientUser.LastName}");
        }
        
    }
    
    [Fact]
    public void AssignedFormInstanceCard_ClickCloseDropdownButton_DropdownCloses()
    {
        CreateComponent();
        _component.Find(".bi-chevron-down").Click();
        _component.Find(".bi-chevron-up").Click();
        _component.FindAll(".project-name").Count.Should().Be(0);

    }
}