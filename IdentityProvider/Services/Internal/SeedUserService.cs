using System;
using System.Threading.Tasks;
using IdentityProvider.Models.Entities;
using Microsoft.AspNetCore.Identity;
using SharedLensResources;

namespace IdentityProvider.Services.Internal;

public interface ISeedUserService
{
    public Task AddTestUsersAsync();
}

public class SeedUserService : ISeedUserService
{
    private readonly UserManager<User> _userManager;

    public SeedUserService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Adds some test users to the database
    /// ONLY TO BE USED IN DEVELOPER ENVIRONMENTS!
    /// </summary>
    public async Task AddTestUsersAsync()
    {
        // If one of our test users is already found in the DB, don't try to make anything happen
        if (await _userManager.FindByNameAsync("aag123") is not null) return;
        
        var regularUser = new User
        {
            Created = DateTime.Now,
            Email = "aag123@scrumboard.com",
            FirstName = "Abby",
            LastName = "Agile",
            UserName = "aag123",
            GlobalLensRole = GlobalLensRole.SystemAdmin,
            IdentitySource = IdentitySource.Lens
        };
        var regularUser2 = new User
        {
            Created = DateTime.Now,
            Email = "ssc123@scrumboard.com",
            FirstName = "Senna",
            LastName = "Scrum",
            UserName = "ssc123",
            GlobalLensRole = GlobalLensRole.User,
            IdentitySource = IdentitySource.Lens
        };
        var adminUser = new User
        {
            Created = DateTime.Now,
            Email = "wwa123@scrumboard.com",
            FirstName = "Walter",
            LastName = "Waterfall",
            UserName = "wwa123",
            GlobalLensRole = GlobalLensRole.SystemAdmin,
            IdentitySource = IdentitySource.Lens
        };
        const string password = "P@ssword123!";
        await _userManager.CreateAsync(regularUser, password);
        await _userManager.CreateAsync(regularUser2, password);
        await _userManager.CreateAsync(adminUser, password);
    }
}