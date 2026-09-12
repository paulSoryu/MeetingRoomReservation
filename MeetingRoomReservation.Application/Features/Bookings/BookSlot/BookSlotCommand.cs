using FluentValidation;
using MediatR;
using MeetingRoomReservation.Application.Features.Bookings;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Bookings;
using MeetingRoomReservation.Domain.Models.Resources;
using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Application.Features.Bookings.BookSlot;

public sealed record BookSlotCommand(
    Guid ResourceId,
    DateTime SlotStart,
    DateTime SlotEnd,
    string UserId) : IRequest<Result<BookingResponse>>;

public sealed class BookSlotCommandValidator : AbstractValidator<BookSlotCommand>
{
    public BookSlotCommandValidator()
    {
        RuleFor(c => c.ResourceId).NotEmpty();
        RuleFor(c => c.SlotEnd).GreaterThan(c => c.SlotStart);
        RuleFor(c => c.UserId).NotEmpty();
    }
}

public sealed class BookSlotCommandHandler(
    IResourceRepository resourceRepository,
    IBookingRepository bookingRepository,
    IBookingNotifier bookingNotifier)
    : IRequestHandler<BookSlotCommand, Result<BookingResponse>>
{
    public async Task<Result<BookingResponse>> Handle(BookSlotCommand request, CancellationToken cancellationToken)
    {
        // Confirms the resource itself exists - unrelated to the slot-conflict race, which is
        // guarded only by the AddAsync insert below, never by a pre-check here.
        var resource = await resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource is null)
            return Result.Failure<BookingResponse>(ResourceErrors.NotFound(request.ResourceId));

        var timeSlotResult = TimeSlot.Create(request.SlotStart, request.SlotEnd);
        if (timeSlotResult.IsFailure)
            return Result.Failure<BookingResponse>(timeSlotResult.Error!);

        var bookingResult = Booking.Create(request.ResourceId, timeSlotResult.Value, request.UserId);
        if (bookingResult.IsFailure)
            return Result.Failure<BookingResponse>(bookingResult.Error!);

        var booking = bookingResult.Value;
        var insertResult = await bookingRepository.AddAsync(booking, cancellationToken);
        if (insertResult.IsFailure)
            return Result.Failure<BookingResponse>(insertResult.Error!);

        await bookingNotifier.NotifyBookingCreatedAsync(booking.ResourceId, booking.TimeSlot, cancellationToken);

        return Result.Success(booking.ToResponse());
    }
}
