using MeetingRoomReservation.Domain.Models.Resources;

namespace MeetingRoomReservation.Application.Interfaces;

public interface IResourceRepository
{
    Task<Resource?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Resource>> ListAsync(CancellationToken cancellationToken);
    Task AddAsync(Resource resource, CancellationToken cancellationToken);
    Task UpdateAsync(Resource resource, CancellationToken cancellationToken);
    Task DeleteAsync(Resource resource, CancellationToken cancellationToken);
}
