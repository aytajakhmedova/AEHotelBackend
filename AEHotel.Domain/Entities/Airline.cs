using AEHotel.Domain.Common;

namespace AEHotel.Domain.Entities;

/// <summary>An airline an admin can pick when creating flights.</summary>
public class Airline : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional IATA code such as TK or J2.</summary>
    public string Code { get; set; } = string.Empty;
}
