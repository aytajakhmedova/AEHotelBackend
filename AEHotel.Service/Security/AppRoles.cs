namespace AEHotel.Service.Security;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Member = "Member";
    public const string User = "User";
    public const string Staff = SuperAdmin + "," + Admin;
}
