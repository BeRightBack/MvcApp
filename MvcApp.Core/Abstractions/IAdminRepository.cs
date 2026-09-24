using MvcApp.Core;
using MvcApp.Core.Models;

namespace MvcApp.Core.Abstractions;

public interface IAdminRepository
{
    Task<List<UserWithRolesModel>> GetUsersWithRolesAsync();
    Task EditRolesAsync(UserWithRolesModel userWithRoles);
    Task<List<string>> GetRolesAsync();
    Task<List<PhotoModel>> GetPhotosForModerationAsync();
}
