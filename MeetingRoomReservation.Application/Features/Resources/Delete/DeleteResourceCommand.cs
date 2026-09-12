using FluentValidation;
using MediatR;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Resources;
using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Application.Features.Resources.Delete;

public sealed record DeleteResourceCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteResourceCommandValidator : AbstractValidator<DeleteResourceCommand>
{
    public DeleteResourceCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}

public sealed class DeleteResourceCommandHandler(IResourceRepository resourceRepository)
    : IRequestHandler<DeleteResourceCommand, Result>
{
    public async Task<Result> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await resourceRepository.GetByIdAsync(request.Id, cancellationToken);
        if (resource is null)
            return Result.Failure(ResourceErrors.NotFound(request.Id));

        await resourceRepository.DeleteAsync(resource, cancellationToken);

        return Result.Success();
    }
}
