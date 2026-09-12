using MeetingRoomReservation.Application.Features.Bookings.BookSlot;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Bookings;
using MeetingRoomReservation.Domain.Models.Resources;
using MeetingRoomReservation.Domain.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MeetingRoomReservation.Tests.Bookings;

public sealed class BookSlotCommandHandlerTests
{
    private readonly Mock<IResourceRepository> _resourceRepository = new();
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IBookingNotifier> _bookingNotifier = new();
    private readonly BookSlotCommandHandler _handler;

    public BookSlotCommandHandlerTests()
    {
        _handler = new BookSlotCommandHandler(
            _resourceRepository.Object,
            _bookingRepository.Object,
            _bookingNotifier.Object,
            NullLogger<BookSlotCommandHandler>.Instance);
    }

    private static Resource CreateResource() =>
        Resource.Create("Room", WorkingHours.Create(new TimeOnly(9, 0), new TimeOnly(17, 0)).Value, TimeSpan.FromMinutes(30)).Value;

    private static BookSlotCommand CreateCommand(Guid resourceId) =>
        new(resourceId, DateTime.Today.AddHours(9), DateTime.Today.AddHours(9).AddMinutes(30), "user-1");

    [Fact]
    public async Task Handle_ResourceDoesNotExist_ReturnsNotFoundErrorAndDoesNotInsert()
    {
        _resourceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource?)null);

        var result = await _handler.Handle(CreateCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
        _bookingRepository.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SlotAlreadyBooked_ReturnsConflictAndDoesNotNotify()
    {
        var resource = CreateResource();
        _resourceRepository.Setup(r => r.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookingRepository
            .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(BookingErrors.SlotAlreadyBooked));

        var result = await _handler.Handle(CreateCommand(resource.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
        _bookingNotifier.Verify(
            n => n.NotifyBookingCreatedAsync(It.IsAny<Guid>(), It.IsAny<TimeSlot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_InsertsBookingNotifiesAndReturnsSuccess()
    {
        var resource = CreateResource();
        _resourceRepository.Setup(r => r.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookingRepository
            .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _handler.Handle(CreateCommand(resource.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("user-1", result.Value.UserId);
        Assert.Equal(resource.Id, result.Value.ResourceId);
        _bookingNotifier.Verify(
            n => n.NotifyBookingCreatedAsync(resource.Id, It.IsAny<TimeSlot>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NotifierThrows_StillReturnsSuccess()
    {
        // Regression test: a real-time notification outage must never turn an already-committed
        // booking into a failed request (see BookSlotCommandHandler's try/catch around the notify call).
        var resource = CreateResource();
        _resourceRepository.Setup(r => r.GetByIdAsync(resource.Id, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookingRepository
            .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _bookingNotifier
            .Setup(n => n.NotifyBookingCreatedAsync(It.IsAny<Guid>(), It.IsAny<TimeSlot>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR unreachable"));

        var result = await _handler.Handle(CreateCommand(resource.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
