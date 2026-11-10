using AEHotel.Service.DTOs.Inquiries;

namespace AEHotel.Service.Services.Interfaces;

public interface IInquiryService
{
    Task<InquiryResponse> CreateAsync(InquiryRequest request, string? userId, CancellationToken cancellationToken = default);

    /// <param name="hotelScopeId">null = all questions (admins); 0 = none; otherwise only that hotel's questions.</param>
    Task<IReadOnlyList<InquiryResponse>> GetAllAsync(int? hotelScopeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InquiryResponse>> GetMineAsync(string userId, CancellationToken cancellationToken = default);

    Task<InquiryResponse> ReplyAsync(int id, string reply, string staffName, int? hotelScopeId, CancellationToken cancellationToken = default);

    Task<InquiryResponse> SetStatusAsync(int id, string status, int? hotelScopeId, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
