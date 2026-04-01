namespace KongPortal.Security;

public static class Roles
{
    public const string Admin    = "Admin";
    public const string Operator = "Operator";
    public const string Viewer   = "Viewer";
}

public static class Policies
{
    public const string CanView    = "CanView";
    public const string CanOperate = "CanOperate";
    public const string CanAdmin   = "CanAdmin";
}

public class AppUser : Microsoft.AspNetCore.Identity.IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
