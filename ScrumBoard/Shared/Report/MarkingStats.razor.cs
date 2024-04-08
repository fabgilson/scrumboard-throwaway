using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Repositories;
using ScrumBoard.Services;
using ScrumBoard.Shared.Marking;

namespace ScrumBoard.Shared.Report;

public partial class MarkingStats : BaseProjectScopedComponent
{
    [Parameter]
    public User SelectedUser { get; set; }
    
    [Inject] 
    protected IUserRepository UserRepository { get; set; }
    
    public async Task ChangeUser(User user)
    {
        SelectedUser = await UserRepository.GetByIdAsync(user.Id);
    }
}