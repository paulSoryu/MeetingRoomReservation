using MeetingRoomReservation.Domain.Models.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeetingRoomReservation.Infrastructure.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.ResourceId)
            .IsRequired();

        builder.Property(booking => booking.UserId)
            .IsRequired();

        builder.Property(booking => booking.CreatedAtUtc)
            .IsRequired();

        builder.OwnsOne(booking => booking.TimeSlot, timeSlot =>
        {
            timeSlot.Property(t => t.SlotStart)
                .HasColumnName("SlotStart")
                .IsRequired();

            timeSlot.Property(t => t.SlotEnd)
                .HasColumnName("SlotEnd")
                .IsRequired();
        });

        // The unique index on (ResourceId, SlotStart) - the single source of truth for
        // "is this slot taken" - cannot be declared here: EF Core's HasIndex can't span a
        // property on the owner (ResourceId) and one on an owned type mapped to the same
        // table (TimeSlot.SlotStart). It's added by raw column name directly in the
        // InitialCreate migration instead; any future migration that touches this table
        // must carry that CreateIndex call forward.
    }
}
