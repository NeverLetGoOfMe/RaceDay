using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? ContactNumber { get; set; }

    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be Organiser or Participant.")]
    public string Role { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? ContactNumber { get; set; }
}

public class EventDto
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? RouteInfo { get; set; }

    [Range(0.1, 1000)]
    public decimal Distance { get; set; }

    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;
}

public class CategoryDto
{
    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }
}

public class EnrolDto
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public class ResultDto
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    public TimeSpan FinishTime { get; set; }

    [Range(1, 100000)]
    public int Position { get; set; }
}
