using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Service.DTOs.Tours;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Service.Services;

public class TourCategoryService : ITourCategoryService
{
    private readonly AppDbContext _db;

    public TourCategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TourCategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _db.TourCategories.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken);
        var counts = await _db.Tours.AsNoTracking()
            .GroupBy(tour => tour.Category)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        return categories
            .Select(item => Map(item, counts.FirstOrDefault(c => string.Equals(c.Name, item.Name, StringComparison.OrdinalIgnoreCase))?.Count ?? 0))
            .ToList();
    }

    public async Task<string?> FindNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var value = (name ?? string.Empty).Trim();
        return await _db.TourCategories.AsNoTracking()
            .Where(item => item.Name == value)
            .Select(item => item.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TourCategoryResponse> CreateAsync(TourCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = Normalize(request);
        if (await _db.TourCategories.AnyAsync(item => item.Name == name, cancellationToken))
        {
            throw new ValidationException("A category with this name already exists.");
        }

        var category = new TourCategory { Name = name };
        _db.TourCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(category, 0);
    }

    public async Task<TourCategoryResponse> UpdateAsync(int id, TourCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var name = Normalize(request);
        var category = await _db.TourCategories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Tour category with id {id} was not found.");
        if (await _db.TourCategories.AnyAsync(item => item.Name == name && item.Id != id, cancellationToken))
        {
            throw new ValidationException("A category with this name already exists.");
        }

        // Tours keep the category by name, so a rename has to follow them.
        var oldName = category.Name;
        var tours = await _db.Tours.Where(tour => tour.Category == oldName).ToListAsync(cancellationToken);
        foreach (var tour in tours)
        {
            tour.Category = name;
        }

        category.Name = name;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(category, tours.Count);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _db.TourCategories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Tour category with id {id} was not found.");
        if (await _db.Tours.AnyAsync(tour => tour.Category == category.Name, cancellationToken))
        {
            throw new ValidationException("This category cannot be deleted because tours use it.");
        }

        _db.TourCategories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string Normalize(TourCategoryRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length is 0 or > 100)
        {
            throw new ValidationException("Category name is required (up to 100 characters).");
        }

        return name;
    }

    private static TourCategoryResponse Map(TourCategory category, int tourCount) => new()
    {
        Id = category.Id,
        Name = category.Name,
        TourCount = tourCount,
    };
}
