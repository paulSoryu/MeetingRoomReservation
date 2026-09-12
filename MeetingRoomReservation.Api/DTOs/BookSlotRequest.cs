namespace MeetingRoomReservation.Api.DTOs;

public sealed record BookSlotRequest(Guid ResourceId, DateTime SlotStart, DateTime SlotEnd);
