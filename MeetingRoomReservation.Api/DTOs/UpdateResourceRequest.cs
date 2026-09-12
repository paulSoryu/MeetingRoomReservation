namespace MeetingRoomReservation.Api.DTOs;

public sealed record UpdateResourceRequest(
    string Name,
    TimeOnly WorkingHoursStart,
    TimeOnly WorkingHoursEnd,
    TimeSpan SlotDuration);
