using MeetingRoomReservation.Application.Features.Bookings.BookSlot;

namespace MeetingRoomReservation.Tests.Bookings;

public sealed class BookSlotCommandValidatorTests
{
    private readonly BookSlotCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var start = DateTime.Today.AddHours(9);
        var command = new BookSlotCommand(Guid.NewGuid(), start, start.AddMinutes(30), "user-1");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyResourceId_HasError()
    {
        var start = DateTime.Today.AddHours(9);
        var command = new BookSlotCommand(Guid.Empty, start, start.AddMinutes(30), "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(BookSlotCommand.ResourceId));
    }

    [Fact]
    public void Validate_SlotEndNotAfterSlotStart_HasError()
    {
        var start = DateTime.Today.AddHours(9);
        var command = new BookSlotCommand(Guid.NewGuid(), start, start, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(BookSlotCommand.SlotEnd));
    }

    [Fact]
    public void Validate_EmptyUserId_HasError()
    {
        var start = DateTime.Today.AddHours(9);
        var command = new BookSlotCommand(Guid.NewGuid(), start, start.AddMinutes(30), "");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(BookSlotCommand.UserId));
    }
}
