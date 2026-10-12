using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Repository.Repositories;

public class FlightRepository : BaseRepository<Flight>, IFlightRepository
{
    public FlightRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Flight>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Flights
            .AsNoTracking()
            .OrderBy(flight => flight.DepartureAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Flight?> GetByIdAsync(int id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Flight> query = Context.Flights;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(flight => flight.Id == id, cancellationToken);
    }
}
