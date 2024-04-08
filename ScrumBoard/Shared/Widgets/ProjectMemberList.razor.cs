using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Shared.Widgets;

public partial class ProjectMemberList
{
    [Parameter]
    public bool OnlyShowSingleRole { get; set; }    
    [Parameter]
    public ProjectRole AllowedRole { get; set; }    
}