using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MeetingRoomReservation.Infrastructure.Data;

// Only used by 'dotnet ef' design-time tooling (migrations add/script/etc.) - never at app runtime,
// where ApplicationDbContext is registered through AddInfrastructure() instead.
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MeetingRoomReservation;Trusted_Connection=True;");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
