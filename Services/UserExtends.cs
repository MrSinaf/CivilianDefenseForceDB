using System.Security.Claims;

namespace DataBaseCDF.Services;

public static class UserExtends
{
    public static string GetId(this ClaimsPrincipal user)
        => user.Claims.FirstOrDefault(c => c.Type == "id")?.Value ?? "";

    public static bool IsAdmin(this ClaimsPrincipal user)
        => bool.TryParse(user.Claims.FirstOrDefault(c => c.Type == "admin")?.Value, out var isAdmin) && isAdmin;

    public static int GetVersion(this ClaimsPrincipal user)
        => int.TryParse(user.Claims.FirstOrDefault(c => c.Type == "version")?.Value, out var version) ? version : 0;
}