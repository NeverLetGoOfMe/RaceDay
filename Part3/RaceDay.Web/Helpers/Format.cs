namespace RaceDay.Web.Helpers;

public static class Format
{
    // 1 -> 1st, 2 -> 2nd, 47 -> 47th ...
    public static string Ordinal(int number)
    {
        var suffix = (number % 100) is >= 11 and <= 13
            ? "th"
            : (number % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{number}{suffix}";
    }

    // Colour-coded enrolment statuses: green Confirmed, amber Pending, red Cancelled
    public static string StatusClass(string status) => status switch
    {
        "Confirmed" => "bg-success",
        "Pending" => "bg-warning text-dark",
        "Cancelled" => "bg-danger",
        _ => "bg-secondary"
    };

    public static string Time(TimeSpan time) => time.ToString(@"hh\:mm\:ss");

    public static string Money(decimal amount) => amount == 0 ? "Free" : $"R{amount:0.##}";
}
