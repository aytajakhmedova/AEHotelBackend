using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

/// <summary>One travel class of a flight (economy, business or first) with its own price and seats.</summary>
public class FlightCabin : BaseEntity
{
    public int FlightId { get; set; }

    public Flight? Flight { get; set; }

    /// <summary>economy, business or first.</summary>
    public string CabinClass { get; set; } = "economy";

    public decimal Price { get; set; }

    public int SeatsAvailable { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
