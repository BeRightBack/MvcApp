using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core.Models;

public class UserWithRolesModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];
}
