using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> entity)
    {
        entity.Property(payment => payment.Method).HasMaxLength(20).IsRequired();
        entity.Property(payment => payment.Status).HasMaxLength(20).IsRequired();
        entity.Property(payment => payment.CardLast4).HasMaxLength(4);
        entity.Property(payment => payment.CardHolder).HasMaxLength(150);
        entity.Property(payment => payment.Amount).HasPrecision(18, 2);

        entity.HasOne(payment => payment.Booking)
            .WithOne(booking => booking.Payment)
            .HasForeignKey<Payment>(payment => payment.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
