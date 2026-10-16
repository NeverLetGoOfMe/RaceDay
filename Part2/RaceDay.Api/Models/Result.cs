namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }

    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }

    public int CapturedByUserId { get; set; }
    public User? CapturedBy { get; set; }

    public DateTime CapturedDate { get; set; } = DateTime.UtcNow;
}
