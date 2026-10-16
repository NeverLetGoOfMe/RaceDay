namespace RaceDay.Api.Dtos;

public record UserResponseDto(int UserId, string FullName, string Email, string? ContactNumber, string Role);

public record EventResponseDto(
    int EventId, int OrganiserId, string EventName, DateTime EventDate, string Location,
    string? Description, string? RouteInfo, decimal Distance, string EventType);

public record CategoryResponseDto(int CategoryId, int EventId, string CategoryName, int MaxParticipants, decimal EntryFee);

public record EnrolmentResponseDto(
    int EnrolmentId, int ParticipantId, string ParticipantName, int EventId, string EventName,
    DateTime EventDate, int CategoryId, string CategoryName, DateTime EnrolmentDate, string Status);

public record ResultResponseDto(
    int ResultId, int EnrolmentId, string ParticipantName, string EventName, string CategoryName,
    TimeSpan FinishTime, int Position);
