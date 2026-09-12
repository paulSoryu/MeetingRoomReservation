namespace MeetingRoomReservation.Infrastructure.Realtime;

public sealed record SlotStatusChangedMessage(Guid ResourceId, DateTime SlotStart, DateTime SlotEnd, bool IsBooked);
