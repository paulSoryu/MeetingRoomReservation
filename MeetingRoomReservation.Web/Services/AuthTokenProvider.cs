namespace MeetingRoomReservation.Web.Services;

// Scoped (one per circuit) - holds the current user's JWT for the lifetime of their Blazor
// Server session. Read by JwtAuthorizationMessageHandler, ApiAuthenticationStateProvider,
// and BookingHubClient.
public sealed class AuthTokenProvider
{
    public string? AccessToken { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    public event Action? Changed;

    public void SetToken(string accessToken, DateTime expiresAtUtc)
    {
        AccessToken = accessToken;
        ExpiresAtUtc = expiresAtUtc;
        Changed?.Invoke();
    }

    public void Clear()
    {
        AccessToken = null;
        ExpiresAtUtc = null;
        Changed?.Invoke();
    }
}
