using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Resources;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomReservation.Infrastructure.Data.Repositories;

public sealed class ResourceRepository(ApplicationDbContext dbContext) : IResourceRepository
{
    public Task<Resource?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Resources.FirstOrDefaultAsync(resource => resource.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Resource>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Resources.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Resource resource, CancellationToken cancellationToken)
    {
        dbContext.Resources.Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Resource resource, CancellationToken cancellationToken)
    {
        dbContext.Resources.Update(resource);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Resource resource, CancellationToken cancellationToken)
    {
        dbContext.Resources.Remove(resource);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
