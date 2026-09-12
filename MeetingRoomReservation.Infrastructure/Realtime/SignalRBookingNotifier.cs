using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Bookings;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace MeetingRoomReservation.Infrastructure.Realtime;

// Talks to the Azure SignalR Service by hub name via the Management SDK instead of
// IHubContext<BookingHub> - that would require a project reference to Api (where the
// Hub class lives), which Clean Architecture's dependency direction doesn't allow.
public sealed class SignalRBookingNotifier(IServiceManager serviceManager) : IBookingNotifier, IAsyncDisposable
{
    public const string HubName = "BookingHub";

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IServiceHubContext? _hubContext;

    public Task NotifyBookingCreatedAsync(Guid resourceId, TimeSlot timeSlot, CancellationToken cancellationToken) =>
        NotifySlotStatusChangedAsync(resourceId, timeSlot, isBooked: true, cancellationToken);

    public Task NotifyBookingCancelledAsync(Guid resourceId, TimeSlot timeSlot, CancellationToken cancellationToken) =>
        NotifySlotStatusChangedAsync(resourceId, timeSlot, isBooked: false, cancellationToken);

    private async Task NotifySlotStatusChangedAsync(
        Guid resourceId,
        TimeSlot timeSlot,
        bool isBooked,
        CancellationToken cancellationToken)
    {
        var hubContext = await GetHubContextAsync(cancellationToken);
        var message = new SlotStatusChangedMessage(resourceId, timeSlot.SlotStart, timeSlot.SlotEnd, isBooked);

        await hubContext.Clients.Group(resourceId.ToString())
            .SendAsync("SlotStatusChanged", message, cancellationToken);
    }

    private async Task<IServiceHubContext> GetHubContextAsync(CancellationToken cancellationToken)
    {
        if (_hubContext is not null)
            return _hubContext;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            return _hubContext ??= await serviceManager.CreateHubContextAsync(HubName, cancellationToken: cancellationToken);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubContext is not null)
            await _hubContext.DisposeAsync();
    }
}
