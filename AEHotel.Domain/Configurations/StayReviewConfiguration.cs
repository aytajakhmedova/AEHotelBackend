using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class StayReviewConfiguration : IEntityTypeConfiguration<StayReview>
{
    public void Configure(EntityTypeBuilder<StayReview> entity)
    {
        entity.Property(item => item.UserId).HasMaxLength(450).IsRequired();
        entity.Property(item => item.GuestName).HasMaxLength(160).IsRequired();
        entity.Property(item => item.Title).HasMaxLength(160).IsRequired();
        entity.Property(item => item.Comment).HasMaxLength(4000).IsRequired();
        entity.HasIndex(item => item.BookingId).IsUnique();
        entity.HasIndex(item => item.HotelId);
        entity.HasOne(item => item.Hotel).WithMany().HasForeignKey(item => item.HotelId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(item => item.Booking).WithMany().HasForeignKey(item => item.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}
