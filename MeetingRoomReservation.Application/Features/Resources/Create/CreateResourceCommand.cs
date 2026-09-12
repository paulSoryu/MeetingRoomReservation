using FluentValidation;
using MediatR;
using MeetingRoomReservation.Application.Features.Resources;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Resources;
using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Application.Features.Resources.Create;

public sealed record CreateResourceCommand(
    string Name,
    TimeOnly WorkingHoursStart,
    TimeOnly WorkingHoursEnd,
    TimeSpan SlotDuration) : IRequest<Result<ResourceResponse>>;

public sealed class CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.WorkingHoursEnd).GreaterThan(c => c.WorkingHoursStart);
        RuleFor(c => c.SlotDuration).GreaterThan(TimeSpan.Zero);
    }
}

public sealed class CreateResourceCommandHandler(IResourceRepository resourceRepository)
    : IRequestHandler<CreateResourceCommand, Result<ResourceResponse>>
{
    public async Task<Result<ResourceResponse>> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
    {
        var workingHoursResult = WorkingHours.Create(request.WorkingHoursStart, request.WorkingHoursEnd);
        if (workingHoursResult.IsFailure)
            return Result.Failure<ResourceResponse>(workingHoursResult.Error!);

        var resourceResult = Resource.Create(request.Name, workingHoursResult.Value, request.SlotDuration);
        if (resourceResult.IsFailure)
            return Result.Failure<ResourceResponse>(resourceResult.Error!);

        var resource = resourceResult.Value;
        await resourceRepository.AddAsync(resource, cancellationToken);

        return Result.Success(resource.ToResponse());
    }
}
