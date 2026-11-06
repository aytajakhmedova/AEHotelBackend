using AEHotel.Service.Services.Interfaces;
using AEHotel.Service.DTOs.Payments;
using AEHotel.Repository.Repositories.Interfaces;
using AEHotel.Service.Exceptions;

namespace AEHotel.Service.Services;

public class PaymentService : IPaymentService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "pending",
        "paid",
        "failed",
        "refunded",
        "cancelled"
    };

    private readonly IPaymentRepository _payments;

    public PaymentService(IPaymentRepository payments)
    {
        _payments = payments;
    }

    public async Task<IReadOnlyList<PaymentResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var payments = await _payments.GetAllAsync(cancellationToken);
        return payments.Select(Map).ToList();
    }

    public async Task<PaymentResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(id, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Payment with id {id} was not found.");
        return Map(payment);
    }

    public async Task<PaymentResponse> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        var next = status.Trim().ToLowerInvariant();
        if (!AllowedStatuses.Contains(next))
        {
            throw new ValidationException("Payment status must be pending, paid, failed, refunded, or cancelled.");
        }

        var payment = await _payments.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Payment with id {id} was not found.");

        // Validate status transition logic
        var currentStatus = payment.Status.ToLowerInvariant();
        
        // Prevent invalid transitions
        if (currentStatus == "refunded" || currentStatus == "cancelled")
        {
            throw new ValidationException("Refunded və ya cancelled ödənişin statusunu dəyişmək olmaz.");
        }

        if (currentStatus == "paid" && next is not ("refunded" or "paid"))
        {
            throw new ValidationException("Paid ödəniş yalnız refunded statusa keçə bilər.");
        }

        // Check booking status compatibility
        if (payment.Booking != null)
        {
            var bookingStatus = payment.Booking.Status.ToLowerInvariant();
            
            if (bookingStatus == "cancelled" && next == "paid")
            {
                throw new ValidationException("Cancelled rezervasiya üçün ödəniş paid edilə bilməz.");
            }

            if (bookingStatus != "cancelled" && next == "refunded")
            {
                throw new ValidationException("Yalnız cancelled rezervasiya üçün refund edilə bilər.");
            }
        }

        payment.Status = next;
        await _payments.SaveChangesAsync(cancellationToken);
        return Map(payment);
    }

    private static PaymentResponse Map(Domain.Entities.Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            BookingCode = payment.Booking?.Code ?? string.Empty,
            GuestName = payment.Booking?.GuestName ?? string.Empty,
            GuestEmail = payment.Booking?.GuestEmail ?? string.Empty,
            ItemName = payment.Booking?.ItemName ?? string.Empty,
            BookingType = payment.Booking?.BookingType ?? string.Empty,
            Method = payment.Method,
            Amount = payment.Amount,
            DepositPercent = payment.DepositPercent,
            DepositAmount = payment.DepositAmount,
            RemainingAmount = payment.Amount - payment.DepositAmount < 0 ? 0 : payment.Amount - payment.DepositAmount,
            Status = payment.Status,
            CardLast4 = payment.CardLast4,
            CardHolder = payment.CardHolder,
            CreatedAt = payment.CreatedAt
        };
    }
}
