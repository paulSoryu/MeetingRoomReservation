using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Bookings;
using MeetingRoomReservation.Domain.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomReservation.Infrastructure.Data.Repositories;

public sealed class BookingRepository(ApplicationDbContext dbContext) : IBookingRepository
{
    // SQL Server error numbers for a unique-index/unique-constraint violation.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Bookings.FirstOrDefaultAsync(booking => booking.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Booking>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Bookings.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Booking>> ListByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Booking>> ListByResourceAndDateAsync(Guid resourceId, DateOnly date, CancellationToken cancellationToken)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = date.ToDateTime(TimeOnly.MaxValue);

        return await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.ResourceId == resourceId
                && booking.TimeSlot.SlotStart >= dayStart
                && booking.TimeSlot.SlotStart <= dayEnd)
            .ToListAsync(cancellationToken);
    }

    public async Task<Result> AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        dbContext.Bookings.Add(booking);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            dbContext.Entry(booking).State = EntityState.Detached;
            return Result.Failure(BookingErrors.SlotAlreadyBooked);
        }
    }

    public async Task DeleteAsync(Booking booking, CancellationToken cancellationToken)
    {
        dbContext.Bookings.Remove(booking);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };
}
