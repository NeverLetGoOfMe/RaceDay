namespace RaceDay.Web.Helpers;

/// <summary>Reads the logged-in user's details from the MVC session.</summary>
public static class WebSession
{
    public static int? UserId(this ISession session) => session.GetInt32("UserId");

    public static string? Role(this ISession session) => session.GetString("Role");

    public static string FullName(this ISession session) => session.GetString("FullName") ?? string.Empty;

    public static string? Picture(this ISession session)
    {
        var picture = session.GetString("ProfilePicture");
        return string.IsNullOrEmpty(picture) ? null : picture;
    }
}
