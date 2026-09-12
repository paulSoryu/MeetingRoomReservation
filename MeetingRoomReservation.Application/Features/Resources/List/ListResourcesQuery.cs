using MediatR;
using MeetingRoomReservation.Application.Features.Resources;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Application.Features.Resources.List;

public sealed record ListResourcesQuery : IRequest<Result<IReadOnlyList<ResourceResponse>>>;

public sealed class ListResourcesQueryHandler(IResourceRepository resourceRepository)
    : IRequestHandler<ListResourcesQuery, Result<IReadOnlyList<ResourceResponse>>>
{
    public async Task<Result<IReadOnlyList<ResourceResponse>>> Handle(ListResourcesQuery request, CancellationToken cancellationToken)
    {
        var resources = await resourceRepository.ListAsync(cancellationToken);
        var responses = resources.Select(resource => resource.ToResponse()).ToList();

        return Result.Success<IReadOnlyList<ResourceResponse>>(responses);
    }
}
