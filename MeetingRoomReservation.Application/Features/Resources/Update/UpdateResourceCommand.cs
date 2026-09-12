using FluentValidation;
using MediatR;
using MeetingRoomReservation.Application.Features.Resources;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Resources;
using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Application.Features.Resources.Update;

public sealed record UpdateResourceCommand(
    Guid Id,
    string Name,
    TimeOnly WorkingHoursStart,
    TimeOnly WorkingHoursEnd,
    TimeSpan SlotDuration) : IRequest<Result<ResourceResponse>>;

public sealed class UpdateResourceCommandValidator : AbstractValidator<UpdateResourceCommand>
{
    public UpdateResourceCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.WorkingHoursEnd).GreaterThan(c => c.WorkingHoursStart);
        RuleFor(c => c.SlotDuration).GreaterThan(TimeSpan.Zero);
    }
}

public sealed class UpdateResourceCommandHandler(IResourceRepository resourceRepository)
    : IRequestHandler<UpdateResourceCommand, Result<ResourceResponse>>
{
    public async Task<Result<ResourceResponse>> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await resourceRepository.GetByIdAsync(request.Id, cancellationToken);
        if (resource is null)
            return Result.Failure<ResourceResponse>(ResourceErrors.NotFound(request.Id));

        var workingHoursResult = WorkingHours.Create(request.WorkingHoursStart, request.WorkingHoursEnd);
        if (workingHoursResult.IsFailure)
            return Result.Failure<ResourceResponse>(workingHoursResult.Error!);

        var updateResult = resource.Update(request.Name, workingHoursResult.Value, request.SlotDuration);
        if (updateResult.IsFailure)
            return Result.Failure<ResourceResponse>(updateResult.Error!);

        await resourceRepository.UpdateAsync(resource, cancellationToken);

        return Result.Success(resource.ToResponse());
    }
}
