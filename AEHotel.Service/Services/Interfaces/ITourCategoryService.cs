using AEHotel.Service.DTOs.Tours;

namespace AEHotel.Service.Services.Interfaces;

public interface ITourCategoryService
{
    Task<IReadOnlyList<TourCategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the stored name of the category, or null if there is no such category.</summary>
    Task<string?> FindNameAsync(string name, CancellationToken cancellationToken = default);

    Task<TourCategoryResponse> CreateAsync(TourCategoryRequest request, CancellationToken cancellationToken = default);

    Task<TourCategoryResponse> UpdateAsync(int id, TourCategoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
