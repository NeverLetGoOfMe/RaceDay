using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    [Required, MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    public int MaxParticipants { get; set; } = 100;

    [Column(TypeName = "decimal(8,2)")]
    public decimal EntryFee { get; set; }

    public List<Enrolment> Enrolments { get; set; } = new();
}
