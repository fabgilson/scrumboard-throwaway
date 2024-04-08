using Bunit;
using FluentAssertions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Shared.Widgets;
using Xunit;

namespace ScrumBoard.Tests.Blazor;

public class CurrentUserAvatarComponentTest : TestContext
{
    private IRenderedComponent<CurrentUserAvatar> _component;

    private readonly User _user = new() { Id = 33, FirstName = "Jeff", LastName="Geoff"};

    private void CreateComponent()
    {
        _component = RenderComponent<CurrentUserAvatar>(parameters =>
            parameters.Add(parameterSelector => parameterSelector.Self, _user)
        );
    }

    [Fact]
    public void CurrentUserAvatar_ComponentCreated_DropdownNotVisible()
    {
        CreateComponent();
        _component.FindAll("#gravatar-hint").Count.Should().Be(0);
    }
    
    [Fact]
    public void CurrentUserAvatar_UserClicked_BecomesVisible()
    {
        CreateComponent();
        
        _component.Find("#click-container").Click();
        
        _component.FindAll("#gravatar-hint").Count.Should().Be(1);
    }
}