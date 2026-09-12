using FluentValidation;
using MediatR;
using MeetingRoomReservation.Application.Features.Resources;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Resources;
using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Application.Features.Resources.GetSchedule;

public sealed record GetResourceScheduleQuery(Guid ResourceId, DateOnly Date) : IRequest<Result<ScheduleResponse>>;

public sealed class GetResourceScheduleQueryValidator : AbstractValidator<GetResourceScheduleQuery>
{
    public GetResourceScheduleQueryValidator()
    {
        RuleFor(q => q.ResourceId).NotEmpty();
    }
}

public sealed class GetResourceScheduleQueryHandler(
    IResourceRepository resourceRepository,
    IBookingRepository bookingRepository)
    : IRequestHandler<GetResourceScheduleQuery, Result<ScheduleResponse>>
{
    public async Task<Result<ScheduleResponse>> Handle(GetResourceScheduleQuery request, CancellationToken cancellationToken)
    {
        var resource = await resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource is null)
            return Result.Failure<ScheduleResponse>(ResourceErrors.NotFound(request.ResourceId));

        var slots = resource.GenerateSlots(request.Date);
        var bookings = await bookingRepository.ListByResourceAndDateAsync(request.ResourceId, request.Date, cancellationToken);
        var bookingBySlotStart = bookings.ToDictionary(booking => booking.TimeSlot.SlotStart);

        var slotResponses = slots
            .Select(slot => bookingBySlotStart.TryGetValue(slot.SlotStart, out var booking)
                ? new SlotResponse(slot.SlotStart, slot.SlotEnd, true, booking.Id)
                : new SlotResponse(slot.SlotStart, slot.SlotEnd, false, null))
            .ToList();

        return Result.Success(new ScheduleResponse(request.ResourceId, request.Date, slotResponses));
    }
}
