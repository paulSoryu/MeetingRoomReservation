using MeetingRoomReservation.Application.Features.Bookings;
using MeetingRoomReservation.Application.Features.Bookings.BookSlot;
using MeetingRoomReservation.Domain.Models.Resources;
using MeetingRoomReservation.Domain.Results;
using MeetingRoomReservation.Infrastructure.Data;
using MeetingRoomReservation.Infrastructure.Data.Repositories;
using MeetingRoomReservation.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MeetingRoomReservation.Tests.Concurrency;

// Integration-style: needs a reachable SQL Server (LocalDB by default, same as
// ApplicationDbContextFactory) - this is the whole point of the test, since the
// unique-index-driven conflict handling in BookingRepository can only be proven against a
// real database, not an in-memory fake.
public sealed class BookingConcurrencyTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=MeetingRoomReservation.ConcurrencyTests;Trusted_Connection=True;TrustServerCertificate=True;";

    private Guid _resourceId;

    public async Task InitializeAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        var workingHours = WorkingHours.Create(new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;
        var resource = Resource.Create("Concurrency Test Room", workingHours, TimeSpan.FromMinutes(30)).Value;
        _resourceId = resource.Id;

        dbContext.Resources.Add(resource);
        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task Handle_ConcurrentBookingsForSameSlot_OnlyOneSucceedsAndRestConflict()
    {
        const int concurrentRequests = 20;
        var slotStart = DateTime.Today.AddDays(1).AddHours(9);
        var slotEnd = slotStart.AddMinutes(30);

        var results = await Task.WhenAll(Enumerable.Range(0, concurrentRequests)
            .Select(i => BookSlotAsync(slotStart, slotEnd, $"user-{i}")));

        Assert.Equal(1, results.Count(r => r.IsSuccess));
        Assert.Equal(concurrentRequests - 1, results.Count(r => r.IsFailure));
        Assert.All(results.Where(r => r.IsFailure), r => Assert.IsType<ConflictError>(r.Error));

        await using var verifyContext = CreateDbContext();
        var bookingCount = await verifyContext.Bookings
            .CountAsync(b => b.ResourceId == _resourceId && b.TimeSlot.SlotStart == slotStart);
        Assert.Equal(1, bookingCount);
    }

    private async Task<Result<BookingResponse>> BookSlotAsync(DateTime slotStart, DateTime slotEnd, string userId)
    {
        // Each simulated request gets its own DbContext/connection, mirroring the scoped
        // DbContext-per-HTTP-request lifetime used in the real app.
        await using var dbContext = CreateDbContext();
        var handler = new BookSlotCommandHandler(
            new ResourceRepository(dbContext),
            new BookingRepository(dbContext),
            new NoOpBookingNotifier(),
            NullLogger<BookSlotCommandHandler>.Instance);

        var command = new BookSlotCommand(_resourceId, slotStart, slotEnd, userId);
        return await handler.Handle(command, CancellationToken.None);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
