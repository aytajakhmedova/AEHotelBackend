namespace AEHotel.Service.DTOs.Users;

public class UserAdminResponse
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = [];

    public int? AssignedHotelId { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class UserRoleRequest
{
    public string Role { get; set; } = string.Empty;

    public int? AssignedHotelId { get; set; }
}
