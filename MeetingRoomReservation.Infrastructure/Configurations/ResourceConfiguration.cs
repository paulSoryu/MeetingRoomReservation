using MeetingRoomReservation.Domain.Models.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeetingRoomReservation.Infrastructure.Configurations;

public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.HasKey(resource => resource.Id);

        builder.Property(resource => resource.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(resource => resource.SlotDuration)
            .IsRequired();

        builder.OwnsOne(resource => resource.WorkingHours, workingHours =>
        {
            workingHours.Property(w => w.StartTime)
                .HasColumnName("WorkingHoursStart")
                .IsRequired();

            workingHours.Property(w => w.EndTime)
                .HasColumnName("WorkingHoursEnd")
                .IsRequired();
        });
    }
}
