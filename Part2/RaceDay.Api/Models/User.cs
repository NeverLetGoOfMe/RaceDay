using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    // Never the plain password - see PasswordHasher
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ContactNumber { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
