using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MeetingRoomReservation.Web.Services;

// AuthTokenProvider is injected directly (not via a DelegatingHandler): handlers registered
// through AddHttpMessageHandler<T> are built and cached by IHttpClientFactory in a separate
// internal scope decoupled from - and outliving - the Blazor circuit that resolved this
// ApiClient, so a scoped token would either go stale or leak across circuits/users.
public sealed class ApiClient(HttpClient httpClient, AuthTokenProvider tokenProvider)
{
    public async Task<LoginResult?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password), cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<LoginResult>(cancellationToken)
            : null;
    }

    public async Task<IReadOnlyList<ResourceDto>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var response = await httpClient.GetAsync("api/resources", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<List<ResourceDto>>(cancellationToken) ?? [];
    }

    public async Task<ScheduleDto?> GetScheduleAsync(Guid resourceId, DateOnly date, CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var response = await httpClient.GetAsync($"api/resources/{resourceId}/schedule?date={date:yyyy-MM-dd}", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ScheduleDto>(cancellationToken)
            : null;
    }

    public Task<HttpResponseMessage> CreateResourceAsync(CreateResourceRequest request, CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        return httpClient.PostAsJsonAsync("api/resources", request, cancellationToken);
    }

    public Task<HttpResponseMessage> UpdateResourceAsync(Guid id, UpdateResourceRequest request, CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        return httpClient.PutAsJsonAsync($"api/resources/{id}", request, cancellationToken);
    }

    public Task<HttpResponseMessage> DeleteResourceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        return httpClient.DeleteAsync($"api/resources/{id}", cancellationToken);
    }

    public Task<HttpResponseMessage> BookSlotAsync(BookSlotRequest request, CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        return httpClient.PostAsJsonAsync("api/bookings", request, cancellationToken);
    }

    public async Task<IReadOnlyList<BookingDto>> GetMyBookingsAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var response = await httpClient.GetAsync("api/bookings/mine", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<List<BookingDto>>(cancellationToken) ?? [];
    }

    public Task<HttpResponseMessage> CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        return httpClient.DeleteAsync($"api/bookings/{bookingId}", cancellationToken);
    }

    private void ApplyAuthHeader() =>
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(tokenProvider.AccessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
}
