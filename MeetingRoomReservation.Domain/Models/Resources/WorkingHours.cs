using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Domain.Models.Resources;

public sealed record WorkingHours
{
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }

    private WorkingHours(TimeOnly startTime, TimeOnly endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    public static Result<WorkingHours> Create(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
            return Result.Failure<WorkingHours>(ResourceErrors.InvalidWorkingHours);

        return Result.Success(new WorkingHours(startTime, endTime));
    }
}
