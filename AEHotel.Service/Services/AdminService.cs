using System.Data;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Service.DTOs.Admin;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Security;
using AEHotel.Service.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Service.Services;

public class AdminService : IAdminService
{
    private static readonly string[] StaffRoles =
    [
        AppRoles.SuperAdmin,
        AppRoles.Admin,
        AppRoles.HotelManager,
        AppRoles.Receptionist
    ];

    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly UserManagementPolicy _management;

    public AdminService(AppDbContext db, UserManager<AppUser> users, UserManagementPolicy management)
    {
        _db = db;
        _users = users;
        _management = management;
    }

    private IQueryable<string> StaffUserIds()
    {
        return from userRole in _db.UserRoles
               join role in _db.Roles on userRole.RoleId equals role.Id
               where StaffRoles.Contains(role.Name!)
               select userRole.UserId;
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var staffIds = StaffUserIds();
        var customers = await _db.Users.AsNoTracking()
            .Where(user => !staffIds.Contains(user.Id))
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync(cancellationToken);

        var stats = await _db.Bookings.AsNoTracking()
            .Where(booking => booking.UserId != null)
            .GroupBy(booking => booking.UserId!)
            .Select(group => new
            {
                UserId = group.Key,
                Count = group.Count(),
                Spent = group.Where(booking => booking.Status != "cancelled").Sum(booking => (decimal?)booking.TotalPrice) ?? 0m,
                Last = group.Max(booking => (DateTime?)booking.CreatedAt)
            })
            .ToDictionaryAsync(item => item.UserId, cancellationToken);

        return customers.Select(user =>
        {
            stats.TryGetValue(user.Id, out var stat);
            return new CustomerResponse
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                IsRestricted = user.IsRestricted,
                CreatedAt = user.CreatedAt,
                BookingCount = stat?.Count ?? 0,
                TotalSpent = stat?.Spent ?? 0m,
                LastBookingAt = stat?.Last
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<CustomerBookingResponse>> GetCustomerBookingsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings.AsNoTracking()
            .Where(booking => booking.UserId == userId)
            .OrderByDescending(booking => booking.CreatedAt)
            .Select(booking => new CustomerBookingResponse
            {
                Id = booking.Id,
                Code = booking.Code,
                BookingType = booking.BookingType,
                ItemName = booking.ItemName,
                Status = booking.Status,
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                TotalPrice = booking.TotalPrice,
                CreatedAt = booking.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StaffResponse>> GetStaffAsync(CancellationToken cancellationToken = default)
    {
        var staffIds = await StaffUserIds().Distinct().ToListAsync(cancellationToken);
        var users = await _db.Users.AsNoTracking()
            .Where(user => staffIds.Contains(user.Id))
            .OrderBy(user => user.Name)
            .ToListAsync(cancellationToken);
        var hotels = await _db.Hotels.AsNoTracking().ToDictionaryAsync(hotel => hotel.Id, hotel => hotel.Title, cancellationToken);

        var result = new List<StaffResponse>(users.Count);
        foreach (var user in users)
        {
            result.Add(await MapStaffAsync(user, hotels));
        }

        return result;
    }

    public async Task<StaffResponse> CreateStaffAsync(StaffRequest request, string currentUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var role = NormalizeRole(request.Role);
        await _management.EnsureCanManageAsync(currentUserId, [], role);

        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ValidationException("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Password is required for a new staff member.");
        }

        if (await _users.FindByEmailAsync(email) is not null)
        {
            throw new ValidationException("A user with this email already exists.");
        }

        var hotelId = await ResolveHotelAsync(role, request.AssignedHotelId, cancellationToken);

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Name = request.Name.Trim(),
            Surname = string.IsNullOrWhiteSpace(request.Surname) ? request.Name.Trim() : request.Surname.Trim(),
            PhoneNumber = request.Phone?.Trim(),
            JobTitle = request.JobTitle?.Trim() ?? string.Empty,
            AssignedHotelId = hotelId,
            IsRestricted = !request.IsActive,
            CreatedAt = DateTime.UtcNow,
            BirthDay = DateTime.UtcNow.Date
        };

        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            throw new ValidationException(string.Join(" ", created.Errors.Select(error => error.Description)));
        }

        var addRoleResult = await _users.AddToRoleAsync(user, role);
        if (!addRoleResult.Succeeded)
        {
            // Rollback: delete the user since role assignment failed
            await _users.DeleteAsync(user);
            throw new ValidationException($"Failed to assign role: {string.Join(" ", addRoleResult.Errors.Select(error => error.Description))}");
        }

        var response = await MapStaffAsync(user, null);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    public async Task<StaffResponse> UpdateStaffAsync(string id, StaffRequest request, string currentUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await _users.FindByIdAsync(id) ?? throw new NotFoundException("Staff member was not found.");
        var currentRoles = await _users.GetRolesAsync(user);
        await _management.EnsureCanManageAsync(currentUserId, currentRoles);
        if (!currentRoles.Any(role => StaffRoles.Contains(role)))
        {
            throw new NotFoundException("Staff member was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Name is required.");
        }

        var role = NormalizeRole(request.Role);
        await _management.EnsureCanManageAsync(currentUserId, currentRoles, role);
        var hotelId = await ResolveHotelAsync(role, request.AssignedHotelId, cancellationToken);

        if (id == currentUserId && (!request.IsActive || !currentRoles.Contains(role)))
        {
            throw new ForbiddenException("You cannot deactivate or change the role of your own account.");
        }

        await _management.EnsureAnotherSuperAdminAsync(user, currentRoles, role, request.IsActive);

        var email = (request.Email ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(email) && !string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _users.FindByEmailAsync(email);
            if (existing is not null && existing.Id != user.Id)
            {
                throw new ValidationException("A user with this email already exists.");
            }

            user.Email = email;
            user.UserName = email;
        }

        user.Name = request.Name.Trim();
        user.Surname = string.IsNullOrWhiteSpace(request.Surname) ? user.Name : request.Surname.Trim();
        user.PhoneNumber = request.Phone?.Trim();
        user.JobTitle = request.JobTitle?.Trim() ?? string.Empty;
        user.AssignedHotelId = hotelId;
        user.IsRestricted = !request.IsActive;

        var updated = await _users.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            throw new ValidationException(string.Join(" ", updated.Errors.Select(error => error.Description)));
        }

        if (!currentRoles.Contains(role) || currentRoles.Count != 1)
        {
            var removeResult = await _users.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                throw new ValidationException($"Failed to remove existing roles: {string.Join(" ", removeResult.Errors.Select(error => error.Description))}");
            }

            var addRoleResult = await _users.AddToRoleAsync(user, role);
            if (!addRoleResult.Succeeded)
            {
                throw new ValidationException($"Failed to assign new role: {string.Join(" ", addRoleResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var reset = await _users.ResetPasswordAsync(user, token, request.Password);
            if (!reset.Succeeded)
            {
                throw new ValidationException(string.Join(" ", reset.Errors.Select(error => error.Description)));
            }
        }

        var response = await MapStaffAsync(user, null);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    public async Task DeleteStaffAsync(string id, string currentUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await _users.FindByIdAsync(id) ?? throw new NotFoundException("Staff member was not found.");
        var roles = await _users.GetRolesAsync(user);
        await _management.EnsureCanManageAsync(currentUserId, roles);
        if (!roles.Any(role => StaffRoles.Contains(role)))
        {
            throw new NotFoundException("Staff member was not found.");
        }

        if (id == currentUserId)
        {
            throw new ForbiddenException("You cannot delete your own account.");
        }

        await _management.EnsureAnotherSuperAdminAsync(user, roles, null, false);

        var deleted = await _users.DeleteAsync(user);
        if (!deleted.Succeeded)
        {
            throw new ValidationException(string.Join(" ", deleted.Errors.Select(error => error.Description)));
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ReportResponse> GetReportAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = to ?? today;
        var start = from ?? new DateOnly(end.Year, end.Month, 1).AddMonths(-11);
        if (start > end)
        {
            throw new ValidationException("The start date must be before the end date.");
        }

        var rangeStart = start.ToDateTime(TimeOnly.MinValue);
        var rangeEnd = end.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var bookings = await _db.Bookings.AsNoTracking()
            .Include(b => b.Payment) // Include payment to check payment status
            .Where(booking => booking.CreatedAt >= rangeStart && booking.CreatedAt < rangeEnd)
            .Select(booking => new { 
                booking.Id, 
                booking.BookingType, 
                booking.Status, 
                booking.HotelId, 
                booking.TotalPrice, 
                booking.CreatedAt,
                PaymentStatus = booking.Payment != null ? booking.Payment.Status : "pending"
            })
            .ToListAsync(cancellationToken);
        
        // Real revenue only includes paid bookings (not just non-cancelled)
        var earning = bookings.Where(booking => booking.Status != "cancelled" && booking.PaymentStatus == "paid").ToList();
        var confirmed = bookings.Where(booking => booking.Status != "cancelled").ToList();

        var report = new ReportResponse
        {
            From = start,
            To = end,
            TotalRevenue = earning.Sum(booking => booking.TotalPrice), // Only paid bookings count as revenue
            BookingCount = bookings.Count,
            CancelledCount = bookings.Count - confirmed.Count,
            AverageBookingValue = confirmed.Count == 0 ? 0 : Math.Round(confirmed.Sum(booking => booking.TotalPrice) / confirmed.Count, 2)
        };

        for (var month = new DateOnly(start.Year, start.Month, 1); month <= end; month = month.AddMonths(1))
        {
            var key = $"{month.Year}-{month.Month:00}";
            var rows = earning.Where(booking => booking.CreatedAt.Year == month.Year && booking.CreatedAt.Month == month.Month).ToList();
            report.Monthly.Add(new ReportMonthRow { Month = key, Revenue = rows.Sum(booking => booking.TotalPrice), Bookings = rows.Count });
        }

        report.ByStatus = bookings.GroupBy(booking => booking.Status)
            .Select(group => new ReportCountRow { Label = group.Key, Count = group.Count() })
            .OrderByDescending(row => row.Count).ToList();

        report.ByType = bookings.GroupBy(booking => booking.BookingType)
            .Select(group => new ReportTypeRow
            {
                Type = group.Key,
                Bookings = group.Count(booking => booking.Status != "cancelled"),
                Revenue = group.Where(booking => booking.Status != "cancelled" && booking.PaymentStatus == "paid").Sum(booking => booking.TotalPrice)
            })
            .OrderByDescending(row => row.Revenue).ToList();

        var hotels = await _db.Hotels.AsNoTracking()
            .Select(hotel => new { hotel.Id, hotel.Title, hotel.IsDeleted, Rooms = hotel.Rooms.Count(room => !room.IsDeleted) })
            .ToListAsync(cancellationToken);

        var stays = await _db.Bookings.AsNoTracking()
            .Where(booking => booking.HotelId != null && booking.Status != "cancelled" && booking.CheckIn != null && booking.CheckOut != null
                && booking.CheckIn <= end && booking.CheckOut > start)
            .Select(booking => new { booking.HotelId, booking.CheckIn, booking.CheckOut })
            .ToListAsync(cancellationToken);

        var days = end.DayNumber - start.DayNumber + 1;
        // a deleted hotel only stays in the list while it still has bookings in the period
        report.Hotels = hotels.Where(hotel => !hotel.IsDeleted || earning.Any(booking => booking.HotelId == hotel.Id)).Select(hotel =>
        {
            var hotelBookings = earning.Where(booking => booking.HotelId == hotel.Id).ToList();
            var nights = stays.Where(stay => stay.HotelId == hotel.Id).Sum(stay =>
            {
                var checkIn = stay.CheckIn!.Value > start ? stay.CheckIn.Value : start;
                var checkOut = stay.CheckOut!.Value <= end.AddDays(1) ? stay.CheckOut.Value : end.AddDays(1);
                return Math.Max(0, checkOut.DayNumber - checkIn.DayNumber);
            });
            var capacity = hotel.Rooms * days;
            return new ReportHotelRow
            {
                HotelId = hotel.Id,
                Name = hotel.Title,
                Bookings = hotelBookings.Count,
                Revenue = hotelBookings.Sum(booking => booking.TotalPrice),
                Rooms = hotel.Rooms,
                BookedNights = nights,
                OccupancyPercent = capacity == 0 ? 0 : Math.Round(Math.Min(100m, nights * 100m / capacity), 1)
            };
        }).OrderByDescending(row => row.Bookings).ThenByDescending(row => row.Revenue).ToList();

        var payments = await _db.Payments.AsNoTracking()
            .Where(payment => payment.CreatedAt >= rangeStart && payment.CreatedAt < rangeEnd)
            .Select(payment => new { payment.Status, payment.Method, payment.Amount })
            .ToListAsync(cancellationToken);
        report.PaymentsByStatus = payments.GroupBy(payment => payment.Status)
            .Select(group => new ReportPaymentRow { Label = group.Key, Count = group.Count(), Amount = group.Sum(payment => payment.Amount) })
            .OrderByDescending(row => row.Amount).ToList();
        report.PaymentsByMethod = payments.GroupBy(payment => payment.Method)
            .Select(group => new ReportPaymentRow { Label = group.Key, Count = group.Count(), Amount = group.Sum(payment => payment.Amount) })
            .OrderByDescending(row => row.Amount).ToList();

        return report;
    }

    private static string NormalizeRole(string? role)
    {
        var value = (role ?? string.Empty).Trim();
        return StaffRoles.FirstOrDefault(item => item.Equals(value, StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException("Role must be SuperAdmin, Admin, HotelManager, or Receptionist.");
    }

    private async Task<int?> ResolveHotelAsync(string role, int? hotelId, CancellationToken cancellationToken)
    {
        if (role is not (AppRoles.HotelManager or AppRoles.Receptionist))
        {
            return null;
        }

        if (hotelId is not > 0)
        {
            throw new ValidationException("A hotel must be assigned to a Hotel Manager or Receptionist.");
        }

        var exists = await _db.Hotels.AsNoTracking().AnyAsync(hotel => hotel.Id == hotelId && !hotel.IsDeleted, cancellationToken);
        return exists ? hotelId : throw new ValidationException("The selected hotel does not exist.");
    }

    private async Task<StaffResponse> MapStaffAsync(AppUser user, IReadOnlyDictionary<int, string>? hotels)
    {
        var roles = await _users.GetRolesAsync(user);
        var hotelName = string.Empty;
        if (user.AssignedHotelId is int hotelId)
        {
            hotelName = hotels is not null && hotels.TryGetValue(hotelId, out var name)
                ? name
                : await _db.Hotels.AsNoTracking().Where(hotel => hotel.Id == hotelId).Select(hotel => hotel.Title).FirstOrDefaultAsync() ?? string.Empty;
        }

        return new StaffResponse
        {
            Id = user.Id,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumber ?? string.Empty,
            JobTitle = user.JobTitle,
            Role = StaffRoles.FirstOrDefault(roles.Contains) ?? roles.FirstOrDefault() ?? string.Empty,
            AssignedHotelId = user.AssignedHotelId,
            AssignedHotelName = hotelName,
            IsActive = !user.IsRestricted,
            CreatedAt = user.CreatedAt
        };
    }
}
