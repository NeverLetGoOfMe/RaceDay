using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Role
{
    public int RoleId { get; set; }

    [Required, MaxLength(20)]
    public string RoleName { get; set; } = string.Empty;

    public List<User> Users { get; set; } = new();
}
