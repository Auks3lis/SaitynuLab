using Lab1.Auth.Model;
using Microsoft.AspNetCore.Identity;

namespace Lab1.Auth;

public class AuthDbSeeder
{
    private readonly UserManager<ForumRestUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthDbSeeder(UserManager<ForumRestUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        await AddDefaultRoles();
        await AddAdminUser();
    }

    private async Task AddDefaultRoles()
    {
        foreach (var role in ForumRoles.All)
        {
            var roleExists = await _roleManager.RoleExistsAsync(role);
            if (!roleExists)
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task AddAdminUser()
    {
        var newAdminUser = new ForumRestUser
        {
            UserName = "admin",
            Email = "admin@admin.com",
        };
        var existingAdminUser = await _userManager.FindByNameAsync(newAdminUser.UserName);
        if (existingAdminUser == null)
        {
            var createAdminResult = await _userManager.CreateAsync(newAdminUser, "AdminPassword1?");
            if (createAdminResult.Succeeded)
            {
                await _userManager.AddToRolesAsync(newAdminUser, ForumRoles.All);
            }
        }
    }
}
