using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }

    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? RouteInfo { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal Distance { get; set; }

    // Run, Walk or Cycle
    [Required, MaxLength(10)]
    public string EventType { get; set; } = "Run";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public List<Category> Categories { get; set; } = new();
}
