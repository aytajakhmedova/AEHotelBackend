using Microsoft.AspNetCore.Identity;

namespace AEHotel.Domain.Entities;

public class AppUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public DateTime BirthDay { get; set; }
    public string? FinKod { get; set; }
    public bool IsRestricted { get; set; } = false;
    public int? AssignedHotelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
