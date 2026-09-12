using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MeetingRoomReservation.Api.Hubs;

[Authorize]
public sealed class BookingHub : Hub
{
    // Group name must match SignalRBookingNotifier's resourceId.ToString() exactly.
    public Task JoinResourceGroup(string resourceId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, resourceId);

    public Task LeaveResourceGroup(string resourceId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, resourceId);
}
