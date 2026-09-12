using MeetingRoomReservation.Application.Features.Bookings.CancelBooking;

namespace MeetingRoomReservation.Tests.Bookings;

public sealed class CancelBookingCommandValidatorTests
{
    private readonly CancelBookingCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var command = new CancelBookingCommand(Guid.NewGuid(), "user-1", IsAdmin: false);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyBookingId_HasError()
    {
        var command = new CancelBookingCommand(Guid.Empty, "user-1", IsAdmin: false);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CancelBookingCommand.BookingId));
    }

    [Fact]
    public void Validate_EmptyRequestingUserId_HasError()
    {
        var command = new CancelBookingCommand(Guid.NewGuid(), "", IsAdmin: false);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CancelBookingCommand.RequestingUserId));
    }
}
