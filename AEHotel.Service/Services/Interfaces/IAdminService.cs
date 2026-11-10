using AEHotel.Service.DTOs.Admin;

namespace AEHotel.Service.Services.Interfaces;

public interface IAdminService
{
    Task<IReadOnlyList<CustomerResponse>> GetCustomersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerBookingResponse>> GetCustomerBookingsAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffResponse>> GetStaffAsync(CancellationToken cancellationToken = default);

    Task<StaffResponse> CreateStaffAsync(StaffRequest request, string currentUserId, CancellationToken cancellationToken = default);

    Task<StaffResponse> UpdateStaffAsync(string id, StaffRequest request, string currentUserId, CancellationToken cancellationToken = default);

    Task DeleteStaffAsync(string id, string currentUserId, CancellationToken cancellationToken = default);

    Task DeleteCustomerAsync(string id, string currentUserId, CancellationToken cancellationToken = default);

    Task<ReportResponse> GetReportAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
}
