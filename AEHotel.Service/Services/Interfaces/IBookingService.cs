using AEHotel.Service.DTOs.Bookings;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Helpers;
namespace AEHotel.Service.Services.Interfaces;

public interface IBookingService
{
    Task<IReadOnlyList<BookingResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<BookingResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<BookingResponse> CreateAsync(BookingRequest request, CancellationToken cancellationToken = default);

    Task<BookingResponse> UpdateAsync(int id, BookingUpdateRequest request, CancellationToken cancellationToken = default);

    Task<BookingResponse> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
