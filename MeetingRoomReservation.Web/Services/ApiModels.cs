namespace MeetingRoomReservation.Web.Services;

// Web's own copy of the wire contract - kept decoupled from Api/Application (Web only ever
// talks to Api over HTTP/SignalR per the architecture, never via a project reference).
public sealed record ResourceDto(Guid Id, string Name, TimeOnly WorkingHoursStart, TimeOnly WorkingHoursEnd, TimeSpan SlotDuration);

public sealed record SlotDto(DateTime SlotStart, DateTime SlotEnd, bool IsBooked, Guid? BookingId);

public sealed record ScheduleDto(Guid ResourceId, DateOnly Date, IReadOnlyList<SlotDto> Slots);

public sealed record BookingDto(Guid Id, Guid ResourceId, DateTime SlotStart, DateTime SlotEnd, string UserId, DateTime CreatedAtUtc);

public sealed record LoginResult(string AccessToken, DateTime ExpiresAtUtc);

public sealed record LoginRequest(string Email, string Password);

public sealed record CreateResourceRequest(string Name, TimeOnly WorkingHoursStart, TimeOnly WorkingHoursEnd, TimeSpan SlotDuration);

public sealed record UpdateResourceRequest(string Name, TimeOnly WorkingHoursStart, TimeOnly WorkingHoursEnd, TimeSpan SlotDuration);

public sealed record BookSlotRequest(Guid ResourceId, DateTime SlotStart, DateTime SlotEnd);

public sealed record SlotStatusChanged(Guid ResourceId, DateTime SlotStart, DateTime SlotEnd, bool IsBooked);
