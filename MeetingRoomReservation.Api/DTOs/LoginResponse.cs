namespace MeetingRoomReservation.Api.DTOs;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);
