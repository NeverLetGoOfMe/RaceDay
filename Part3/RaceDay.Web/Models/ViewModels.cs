using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginVm
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That does not look like an email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterVm
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That does not look like an email address.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "That does not look like a phone number.")]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [Required(ErrorMessage = "Please choose a password."), MinLength(8, ErrorMessage = "Your password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password."), Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a role.")]
    [Display(Name = "I am registering as")]
    public string Role { get; set; } = "Participant";
}

public class EventFormVm
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Route information")]
    public string? RouteInfo { get; set; }

    [Required(ErrorMessage = "Please enter the distance."), Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    public string? ExistingBanner { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }
}

public class CategoryFormVm
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter a category name (for example Under 20, Senior or 10km)."), StringLength(50)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Enter a number of places between 1 and 100000.")]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 100000, ErrorMessage = "Enter an entry fee of 0 or more.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }
}

public class ProfileVm
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "That does not look like a phone number.")]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
}

public class ResultFormVm
{
    public int EventId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a participant.")]
    public int EnrolmentId { get; set; }

    [Required(ErrorMessage = "Enter the finish time as hh:mm:ss, for example 00:52:10.")]
    [Display(Name = "Finish time")]
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 100000, ErrorMessage = "Enter a finishing position of 1 or more.")]
    [Display(Name = "Finishing position")]
    public int Position { get; set; }
}

// Page models

public class EventsIndexVm
{
    public List<ApiEvent> Events { get; set; } = new();
    public string? Type { get; set; }
    public string? Search { get; set; }
    public DateTime From { get; set; }
}

public class EventDetailsVm
{
    public ApiEvent Event { get; set; } = new();
    public List<ApiCategory> Categories { get; set; } = new();
}

public class CategoriesPageVm
{
    public ApiEvent Event { get; set; } = new();
    public List<ApiCategory> Categories { get; set; } = new();
    public CategoryFormVm Form { get; set; } = new();
}

public class EventEnrolmentsVm
{
    public ApiEvent Event { get; set; } = new();
    public List<ApiEnrolment> Enrolments { get; set; } = new();
}

public class ResultsPageVm
{
    public ApiEvent Event { get; set; } = new();
    public List<ApiEnrolment> Enrolments { get; set; } = new();
    public Dictionary<int, ApiRaceResult> ResultsByEnrolment { get; set; } = new();
    public ResultFormVm Form { get; set; } = new();
}
