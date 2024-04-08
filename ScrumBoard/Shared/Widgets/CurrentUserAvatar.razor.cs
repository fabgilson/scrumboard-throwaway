using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Shared.Widgets;

public partial class CurrentUserAvatar
{
    [CascadingParameter(Name = "Self")]
    public User Self { get; set; }
    
    private bool _showGravatarInfo = false;
}