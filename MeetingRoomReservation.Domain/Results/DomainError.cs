namespace MeetingRoomReservation.Domain.Results;

public abstract record DomainError(string Code, string Message);

public sealed record ValidationError(string Code, string Message) : DomainError(Code, Message);

public sealed record NotFoundError(string Code, string Message) : DomainError(Code, Message);

public sealed record ConflictError(string Code, string Message) : DomainError(Code, Message);
