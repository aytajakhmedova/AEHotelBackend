using AEHotel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AEHotel.Domain.Configurations;

public class InquiryConfiguration : IEntityTypeConfiguration<Inquiry>
{
    public void Configure(EntityTypeBuilder<Inquiry> entity)
    {
        entity.Property(item => item.Name).HasMaxLength(100).IsRequired();
        entity.Property(item => item.Email).HasMaxLength(200).IsRequired();
        entity.Property(item => item.Subject).HasMaxLength(150).IsRequired();
        entity.Property(item => item.Message).HasMaxLength(3000).IsRequired();
        entity.Property(item => item.Status).HasMaxLength(20).IsRequired();
        entity.Property(item => item.Reply).HasMaxLength(4000);
        entity.Property(item => item.RepliedBy).HasMaxLength(200);
        entity.Property(item => item.UserId).HasMaxLength(450);
        entity.HasIndex(item => new { item.HotelId, item.Status });
        entity.HasOne(item => item.Hotel)
            .WithMany()
            .HasForeignKey(item => item.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
