namespace AEHotel.Service.Security;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string HotelManager = "HotelManager";
    public const string Receptionist = "Receptionist";
    public const string Customer = "Customer";
    public const string Member = "Member";
    public const string User = "User";
    public const string Staff = SuperAdmin + "," + Admin;
    public const string CatalogEditors = SuperAdmin + "," + Admin + "," + HotelManager;
    public const string BookingOps = SuperAdmin + "," + Admin + "," + HotelManager + "," + Receptionist;
}
