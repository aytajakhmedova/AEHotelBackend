using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Repositories;

public class TourRepository : BaseRepository<Tour>, ITourRepository
{
    public TourRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Tour>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Tours.AsNoTracking().OrderBy(tour => tour.Id).ToListAsync(cancellationToken);
    }

    public Task<Tour?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Tour> query = Context.Tours;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(tour => tour.Id == id, cancellationToken);
    }
}
