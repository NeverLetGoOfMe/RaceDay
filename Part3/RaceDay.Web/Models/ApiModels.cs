namespace RaceDay.Web.Models;

// Shapes of the data the Part 2 API sends back. They only carry data - no business logic lives in the MVC app.

public class ApiLogin
{
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class ApiUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
}

public class ApiEvent
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RouteInfo { get; set; }
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImage { get; set; }
}

public class ApiEventSummary
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string? BannerImage { get; set; }
    public int EnrolmentCount { get; set; }
}

public class ApiCategory
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class ApiEnrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ApiRaceResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public int TotalFinishers { get; set; }
}

public class ApiImage
{
    public string? Image { get; set; }
}
