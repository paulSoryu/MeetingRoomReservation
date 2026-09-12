using MeetingRoomReservation.Application.Features.Bookings.CancelBooking;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Bookings;
using MeetingRoomReservation.Domain.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MeetingRoomReservation.Tests.Bookings;

public sealed class CancelBookingCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IBookingNotifier> _bookingNotifier = new();
    private readonly CancelBookingCommandHandler _handler;

    public CancelBookingCommandHandlerTests()
    {
        _handler = new CancelBookingCommandHandler(
            _bookingRepository.Object,
            _bookingNotifier.Object,
            NullLogger<CancelBookingCommandHandler>.Instance);
    }

    private static Booking CreateBooking(string ownerUserId) =>
        Booking.Create(
            Guid.NewGuid(),
            TimeSlot.Create(DateTime.Today.AddHours(9), DateTime.Today.AddHours(9).AddMinutes(30)).Value,
            ownerUserId).Value;

    [Fact]
    public async Task Handle_BookingDoesNotExist_ReturnsNotFoundError()
    {
        _bookingRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        var result = await _handler.Handle(new CancelBookingCommand(Guid.NewGuid(), "user-1", IsAdmin: false), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
        _bookingRepository.Verify(r => r.DeleteAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonOwnerNonAdmin_ReturnsNotFoundErrorAndDoesNotDelete()
    {
        var booking = CreateBooking(ownerUserId: "owner");
        _bookingRepository.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var result = await _handler.Handle(new CancelBookingCommand(booking.Id, "someone-else", IsAdmin: false), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
        _bookingRepository.Verify(r => r.DeleteAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Owner_DeletesNotifiesAndReturnsSuccess()
    {
        var booking = CreateBooking(ownerUserId: "owner");
        _bookingRepository.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var result = await _handler.Handle(new CancelBookingCommand(booking.Id, "owner", IsAdmin: false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _bookingRepository.Verify(r => r.DeleteAsync(booking, It.IsAny<CancellationToken>()), Times.Once);
        _bookingNotifier.Verify(
            n => n.NotifyBookingCancelledAsync(booking.ResourceId, booking.TimeSlot, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AdminCancelsAnothersBooking_ReturnsSuccess()
    {
        var booking = CreateBooking(ownerUserId: "owner");
        _bookingRepository.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var result = await _handler.Handle(new CancelBookingCommand(booking.Id, "admin-user", IsAdmin: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _bookingRepository.Verify(r => r.DeleteAsync(booking, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotifierThrows_StillReturnsSuccess()
    {
        // Regression test: a real-time notification outage must never turn an already-committed
        // cancellation into a failed request (see CancelBookingCommandHandler's try/catch).
        var booking = CreateBooking(ownerUserId: "owner");
        _bookingRepository.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _bookingNotifier
            .Setup(n => n.NotifyBookingCancelledAsync(It.IsAny<Guid>(), It.IsAny<TimeSlot>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR unreachable"));

        var result = await _handler.Handle(new CancelBookingCommand(booking.Id, "owner", IsAdmin: false), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
