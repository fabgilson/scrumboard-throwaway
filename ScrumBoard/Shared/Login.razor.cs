using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ScrumBoard.Repositories;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;
using ScrumBoard.Services.StateStorage;
using System;
using System.Threading.Tasks;
using System.Linq;
using ScrumBoard.Services.UsageData;
using ScrumBoard.Models.Entities.UsageData;
using ScrumBoard.Models.Forms;
using ScrumBoard.Pages;
using SharedLensResources;
using SharedLensResources.Authentication;

namespace ScrumBoard.Shared
{
    public partial class Login
    {
        private string _errorText;

        protected LoginForm LoginForm { get; } = new (true);

        [Inject]
        protected ILogger<Login> Logger { get; set; }

        [Inject]
        protected IAuthenticationService AuthenticationService { get; set; }

        [Inject]
        protected IUserRepository UserRepository { get; set; }

        [Inject]
        protected ISeedDataService SeedDataService { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected IScrumBoardStateStorageService StateStorageService { get; set; } 

        [Inject]
        protected ISprintRepository SprintRepository { get; set; }

        [Inject]
        protected IConfigurationService ConfigurationService { get; set; }

        [Inject]
        protected IUsageDataService UsageDataService { get; set; }

        /// <summary>
        /// Authenticate with IdentityProvider given the provided credentials. If login attempt is successful, take
        /// user to the homepage, if not, show the error message.
        /// </summary>
        protected async Task AttemptLogin()
        {
            var loginAttempt = await AuthenticationService.AttemptLogin(LoginForm.AsLensAuthenticateRequest);
            if (loginAttempt.Success)
            {
                Logger.LogInformation("User (Id={LoginAttemptUserId}) logged in successfully", loginAttempt.UserResponse.Id);
                var foundUser = await UserRepository.GetByIdAsync(loginAttempt.UserResponse.Id);
                // Save (add if not exists, or update) the user information returned in response 
                var user = new User { 
                    Id = loginAttempt.UserResponse.Id, 
                    FirstName = loginAttempt.UserResponse.FirstName, 
                    LastName = loginAttempt.UserResponse.LastName,
                    Email = loginAttempt.UserResponse.Email,
                    LDAPUsername = loginAttempt.UserResponse.UserName
                };                                                                           
                if (foundUser is null) { 
                    Logger.LogInformation("Creating User (name={LoginAttemptFirstName} {LoginAttemptLastName})", 
                        loginAttempt.UserResponse.FirstName, loginAttempt.UserResponse.LastName);
                    await UserRepository.AddAsync(user);
                    if (ConfigurationService.SeedDataEnabled) {
                        await SeedDataService.AddUserToGeneratedProjects(user); 
                    }                        
                } else {
                    Logger.LogInformation("Updating User (name={LoginAttemptFirstName} {LoginAttemptLastName})", 
                        loginAttempt.UserResponse.FirstName, loginAttempt.UserResponse.LastName);
                    await UserRepository.UpdateAsync(user);  
                }

                UsageDataService.AddUsageEvent(new AuthenticationUsageEvent(user.Id, AuthenticationUsageEventType.LogIn));

                long? selectedProjectId = await StateStorageService.GetSelectedProjectIdAsync();               
                if (selectedProjectId.HasValue) {
                    var currentSprints = await SprintRepository.GetByProjectIdAsync(selectedProjectId.Value);                    
                    if (currentSprints.Any(s => s.Stage == SprintStage.Started)) {                       
                        NavigationManager.NavigateTo(PageRoutes.ToProjectSprintBoard(selectedProjectId.Value), true);
                    } else {
                        NavigationManager.NavigateTo(PageRoutes.ToProjectBacklog(selectedProjectId.Value), true);
                    }
                } else {
                    NavigationManager.NavigateTo("", true);
                }
                
            }
            else
            {
                Logger.LogInformation("User failed to login");
                _errorText = loginAttempt.Message;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Property for when to show the error label (whenever the error-text is not blank)
        /// </summary>
        private bool HideError => string.IsNullOrWhiteSpace(_errorText);
    }
}
