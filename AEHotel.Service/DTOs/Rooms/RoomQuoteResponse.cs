namespace AEHotel.Service.DTOs.Rooms;

public class RoomQuoteResponse
{
    public bool Available { get; set; }

    public string Message { get; set; } = string.Empty;

    public int Nights { get; set; }

    public decimal BasePrice { get; set; }

    public decimal ExtraGuestFee { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }
}
