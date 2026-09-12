using System.Security.Claims;
using Mapster;
using MediatR;
using MeetingRoomReservation.Api.DTOs;
using MeetingRoomReservation.Application.Features.Bookings.BookSlot;
using MeetingRoomReservation.Application.Features.Bookings.CancelBooking;
using MeetingRoomReservation.Application.Features.Bookings.GetBookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomReservation.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public sealed class BookingsController(ISender sender) : ApiControllerBase
{
    /// <summary>Books a free slot.</summary>
    [HttpPost]
    public async Task<IActionResult> Book(BookSlotRequest request, CancellationToken cancellationToken)
    {
        var command = request.Adapt<BookSlotCommand>() with { UserId = CurrentUserId };
        var result = await sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Gets the current user's bookings.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBookingsQuery(CurrentUserId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Gets all bookings across all users.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> All(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBookingsQuery(null), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Cancels a booking. Only the owning user or an Admin may cancel.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await sender.Send(new CancelBookingCommand(id, CurrentUserId, isAdmin), cancellationToken);
        return HandleResult(result);
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The current user does not have a NameIdentifier claim.");
}
