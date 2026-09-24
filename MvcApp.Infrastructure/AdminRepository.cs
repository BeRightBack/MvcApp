using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Models;
using MvcApp.Infrastructure.Helpers;

namespace MvcApp.Infrastructure;

public class AdminRepository(
    UserManager<UserDetails> userManager,
    RoleManager<UserRole> roleManager,
    UserDbContext context) : IAdminRepository
{
    public async Task<List<string>> GetRolesAsync()
    {
        return await roleManager.Roles
            .Select(x => x.Name!)
            .OrderBy(x => x)
            .ToListAsync();
    }

    public async Task<List<UserWithRolesModel>> GetUsersWithRolesAsync()
    {
        var users = await userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();

        List<UserWithRolesModel> usersWithRoles = [];

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            usersWithRoles.Add(new UserWithRolesModel
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Roles = [.. roles]
            });
        }

        return usersWithRoles;
    }

    public async Task EditRolesAsync(UserWithRolesModel userWithRoles)
    {
        if (string.IsNullOrWhiteSpace(userWithRoles.Username))
        {
            throw new ArgumentException("Invalid user for role modification");
        }

        UserDetails? user = await userManager.FindByNameAsync(userWithRoles.Username);

        if (user is null)
        {
            throw new ArgumentException($"Could not find user [{userWithRoles.Username}]");
        }

        IList<string> userRoles = await userManager.GetRolesAsync(user);
        IdentityResult result = await userManager.AddToRolesAsync(user, userWithRoles.Roles.Except(userRoles));

        if (result.Succeeded == false)
        {
            throw new Exception($"Failed to add user [{userWithRoles.Username}] to roles [{userWithRoles.Roles.Except(userRoles)}]");
        }

        result = await userManager.RemoveFromRolesAsync(user, userRoles.Except(userWithRoles.Roles));

        if (result.Succeeded == false)
        {
            throw new Exception($"Failed to remove user [{userWithRoles.Username}] from roles [{userRoles.Except(userWithRoles.Roles)}]");
        }
    }

    public async Task<List<PhotoModel>> GetPhotosForModerationAsync()
    {
        var users = await context.Users
            .Include(u => u.Photos)
            .AsNoTracking()
            .ToListAsync();

        var photos = users.SelectMany(u => u.Photos.Select(p => EntityMapper.MapToPhotoModel(p, u.UserName ?? string.Empty)))
            .OfType<PhotoModel>()
            .ToList();

        return photos;
    }
}
