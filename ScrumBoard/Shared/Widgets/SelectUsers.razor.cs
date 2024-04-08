using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Shared.Modals;

namespace ScrumBoard.Shared.Widgets;

public partial class SelectUsers : ComponentBase
{
    [CascadingParameter]
    public Project Project { get; set; }
    
    // Currently logged in user
    [CascadingParameter(Name = "Self")]
    public User Self { get; set; }
    
    [Parameter]
    public List<User> Users { get; set; } = new List<User>();

    [Parameter]
    public List<User> SelectedUsers { get; set; } = new List<User>();

    [Parameter]
    public EventCallback<(List<User>, ICollection<ProjectUserMembership>)> UserSelected { get; set; }

    [Parameter]
    public EventCallback<ICollection<ProjectUserMembership>> RoleChanged { get; set; }

    private ICollection<ProjectUserMembership> _projectAssociations = new List<ProjectUserMembership>();

    [Parameter]
    public bool HasRoleChanger { get; set; }               

    private RemoveUserModal _modal;

    private bool _firstLoad = true;

    protected override void OnInitialized()
    {
        if (!HasRoleChanger || !_firstLoad) return;
        // Deep copy the associations so if roles are updated here, the project associations in the parent are not.
        // This allowed for the ProjectEditView to know the previous state of each user's role for changelog purposes
        _projectAssociations = Project.MemberAssociations.ToList();
        _firstLoad = false;
    }

    private void SelectUser(User user)
    {
        if (HasRoleChanger) {
            var newMembership = new ProjectUserMembership() { 
                Project = Project, 
                User = user, 
                ProjectId = Project.Id, 
                UserId = user.Id, 
                Role = ProjectRole.Guest
            };
            var associationsList = _projectAssociations.ToList();
            associationsList.Add(newMembership);
            _projectAssociations = associationsList;
        }            

        SelectedUsers.Add(user);
        UserSelected.InvokeAsync((SelectedUsers, _projectAssociations));
    }

    private async Task ConfirmRemoval(User user) {
        var cancelled = await _modal.Show(user);
        if (cancelled) return;

        SelectedUsers.Remove(user);
        _projectAssociations = _projectAssociations.Where(a => a.UserId != user.Id).ToList();
        await UserSelected.InvokeAsync((SelectedUsers, _projectAssociations));
    }

    private void ChangeRole(ChangeEventArgs e, User user) {

        ProjectRole role = Enum.Parse<ProjectRole>(e.Value.ToString());        
            
        ProjectUserMembership association = _projectAssociations.First(a => a.UserId == user.Id);
        association.Role = role;
        _projectAssociations.First(assoc => assoc.UserId == user.Id).Role = role;
        RoleChanged.InvokeAsync(_projectAssociations);                               
    }

    private bool IsSelected(User user, ProjectRole role) {                      
        return _projectAssociations
            .Where(association => association.UserId == user.Id)
            .Select(association => association.Role)
            .FirstOrDefault(ProjectRole.Guest) == role;            
    }        
}