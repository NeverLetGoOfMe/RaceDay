namespace RaceDay.Api.Helpers;

/// <summary>Small helpers so the session keys are only written in one place.</summary>
public static class UserSessionExtensions
{
    private const string UserIdKey = "UserId";
    private const string RoleKey = "Role";

    public static void SetUser(this ISession session, int userId, string role)
    {
        session.SetInt32(UserIdKey, userId);
        session.SetString(RoleKey, role);
    }

    public static int? GetUserId(this ISession session) => session.GetInt32(UserIdKey);

    public static string? GetRole(this ISession session) => session.GetString(RoleKey);
}
