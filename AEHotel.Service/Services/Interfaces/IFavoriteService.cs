namespace AEHotel.Service.Services.Interfaces;

public interface IFavoriteService
{
    /// <summary>The ids of the user's favourite hotels (hotels that were deleted are left out).</summary>
    Task<IReadOnlyList<int>> GetHotelIdsAsync(string userId, CancellationToken cancellationToken = default);

    Task AddAsync(string userId, int hotelId, CancellationToken cancellationToken = default);

    Task RemoveAsync(string userId, int hotelId, CancellationToken cancellationToken = default);
}
