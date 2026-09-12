using Microsoft.AspNetCore.SignalR.Client;

namespace MeetingRoomReservation.Web.Services;

// Scoped (one per circuit) - separate from Blazor Server's own internal SignalR circuit,
// this is the client-side connection to Api's BookingHub for real-time slot-status updates.
public sealed class BookingHubClient : IAsyncDisposable
{
    private readonly HubConnection _connection;

    public event Action<SlotStatusChanged>? SlotStatusChanged;

    public BookingHubClient(IConfiguration configuration, AuthTokenProvider tokenProvider)
    {
        var apiBaseUrl = configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl configuration is required.");
        var hubUrl = $"{apiBaseUrl.TrimEnd('/')}/hubs/booking";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(tokenProvider.AccessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<SlotStatusChanged>("SlotStatusChanged", message => SlotStatusChanged?.Invoke(message));
    }

    public async Task JoinResourceGroupAsync(Guid resourceId)
    {
        if (_connection.State == HubConnectionState.Disconnected)
            await _connection.StartAsync();

        await _connection.InvokeAsync("JoinResourceGroup", resourceId.ToString());
    }

    public async Task LeaveResourceGroupAsync(Guid resourceId)
    {
        if (_connection.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("LeaveResourceGroup", resourceId.ToString());
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
