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
            throw new ValidationException("Payment status must be pending, paid, refunded, or cancelled.");
        }

        var payment = await _payments.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Payment with id {id} was not found.");
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
