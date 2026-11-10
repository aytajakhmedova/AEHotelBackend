using System.Net;
using AEHotel.Domain.Entities;
using AEHotel.Repository.Data;
using AEHotel.Service.DTOs.Inquiries;
using AEHotel.Service.Exceptions;
using AEHotel.Service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AEHotel.Service.Services;

public class InquiryService : IInquiryService
{
    private static readonly string[] Statuses = ["new", "answered", "closed"];

    private readonly AppDbContext _db;
    private readonly IEmailSender _email;

    public InquiryService(AppDbContext db, IEmailSender email)
    {
        _db = db;
        _email = email;
    }

    public async Task<InquiryResponse> CreateAsync(InquiryRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim();
        var subject = (request.Subject ?? string.Empty).Trim();
        var message = (request.Message ?? string.Empty).Trim();

        if (name.Length is < 2 or > 100) throw new ValidationException("Ad 2-100 simvol olmalıdır.");
        if (email.Length > 200 || !System.Net.Mail.MailAddress.TryCreate(email, out _)) throw new ValidationException("E-poçt ünvanı düzgün deyil.");
        if (subject.Length is < 3 or > 150) throw new ValidationException("Mövzu 3-150 simvol olmalıdır.");
        if (message.Length is < 10 or > 3000) throw new ValidationException("Mesaj 10-3000 simvol olmalıdır.");

        Hotel? hotel = null;
        if (request.HotelId.HasValue)
        {
            hotel = await _db.Hotels.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.HotelId.Value && !item.IsDeleted, cancellationToken)
                ?? throw new ValidationException("Seçilən otel tapılmadı.");
        }

        var inquiry = new Inquiry
        {
            HotelId = hotel?.Id,
            UserId = userId,
            Name = name,
            Email = email,
            Subject = subject,
            Message = message,
            Status = "new",
            CreatedAt = DateTime.UtcNow,
        };
        _db.Inquiries.Add(inquiry);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(inquiry, hotel?.Title);
    }

    public async Task<IReadOnlyList<InquiryResponse>> GetAllAsync(int? hotelScopeId, CancellationToken cancellationToken = default)
    {
        var query = _db.Inquiries.AsNoTracking().Include(item => item.Hotel).AsQueryable();
        if (hotelScopeId.HasValue)
        {
            query = hotelScopeId.Value == 0
                ? query.Where(_ => false)
                : query.Where(item => item.HotelId == hotelScopeId.Value);
        }

        var items = await query.OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken);
        return items.Select(item => Map(item, item.Hotel?.Title)).ToList();
    }

    public async Task<IReadOnlyList<InquiryResponse>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = await _db.Inquiries.AsNoTracking().Include(item => item.Hotel)
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(item => Map(item, item.Hotel?.Title)).ToList();
    }

    public async Task<InquiryResponse> ReplyAsync(int id, string reply, string staffName, int? hotelScopeId, CancellationToken cancellationToken = default)
    {
        reply = (reply ?? string.Empty).Trim();
        if (reply.Length is < 2 or > 4000) throw new ValidationException("Cavab 2-4000 simvol olmalıdır.");

        var inquiry = await FindScopedAsync(id, hotelScopeId, cancellationToken);
        inquiry.Reply = reply;
        inquiry.RepliedBy = staffName;
        inquiry.RepliedAt = DateTime.UtcNow;
        inquiry.Status = "answered";
        await _db.SaveChangesAsync(cancellationToken);

        // The answer is saved first; a mail failure must not lose it (a signed-in guest can still read it under "my questions").
        try
        {
            if (_email.IsConfigured)
            {
                var place = inquiry.Hotel?.Title is { Length: > 0 } title ? title : "AE Hotel";
                var body = $"<p>Salam, {WebUtility.HtmlEncode(inquiry.Name)},</p>" +
                           $"<p>\"{WebUtility.HtmlEncode(inquiry.Subject)}\" sualınıza cavab:</p>" +
                           $"<blockquote>{WebUtility.HtmlEncode(inquiry.Reply).Replace("\n", "<br>")}</blockquote>" +
                           $"<p>{WebUtility.HtmlEncode(place)}</p>";
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                await _email.SendAsync(inquiry.Email, $"{place}: sualınıza cavab", body, cancellationToken: timeout.Token);
            }
        }
        catch (Exception)
        {
            // Intentionally ignored: see comment above.
        }

        return Map(inquiry, inquiry.Hotel?.Title);
    }

    public async Task<InquiryResponse> SetStatusAsync(int id, string status, int? hotelScopeId, CancellationToken cancellationToken = default)
    {
        status = (status ?? string.Empty).Trim().ToLowerInvariant();
        if (!Statuses.Contains(status)) throw new ValidationException("Status new, answered və ya closed olmalıdır.");

        var inquiry = await FindScopedAsync(id, hotelScopeId, cancellationToken);
        inquiry.Status = status;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(inquiry, inquiry.Hotel?.Title);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var inquiry = await _db.Inquiries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Sual {id} tapılmadı.");
        _db.Inquiries.Remove(inquiry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // A manager may only touch questions of their own hotel; to them any other question simply does not exist.
    private async Task<Inquiry> FindScopedAsync(int id, int? hotelScopeId, CancellationToken cancellationToken)
    {
        var inquiry = await _db.Inquiries.Include(item => item.Hotel).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (inquiry is null || (hotelScopeId.HasValue && (hotelScopeId.Value == 0 || inquiry.HotelId != hotelScopeId.Value)))
        {
            throw new NotFoundException($"Sual {id} tapılmadı.");
        }

        return inquiry;
    }

    private static InquiryResponse Map(Inquiry item, string? hotelName) => new()
    {
        Id = item.Id,
        HotelId = item.HotelId,
        HotelName = hotelName ?? string.Empty,
        Name = item.Name,
        Email = item.Email,
        Subject = item.Subject,
        Message = item.Message,
        Status = item.Status,
        Reply = item.Reply,
        RepliedBy = item.RepliedBy,
        RepliedAt = item.RepliedAt,
        CreatedAt = item.CreatedAt,
    };
}
