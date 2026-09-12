using Mapster;
using MediatR;
using MeetingRoomReservation.Api.DTOs;
using MeetingRoomReservation.Application.Features.Resources.Create;
using MeetingRoomReservation.Application.Features.Resources.Delete;
using MeetingRoomReservation.Application.Features.Resources.GetSchedule;
using MeetingRoomReservation.Application.Features.Resources.List;
using MeetingRoomReservation.Application.Features.Resources.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomReservation.Api.Controllers;

[ApiController]
[Route("api/resources")]
[Authorize]
public sealed class ResourcesController(ISender sender) : ApiControllerBase
{
    /// <summary>Lists all bookable resources.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListResourcesQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Gets the free/booked slot schedule for a resource on a given date.</summary>
    [HttpGet("{id:guid}/schedule")]
    public async Task<IActionResult> GetSchedule(Guid id, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetResourceScheduleQuery(id, date), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Creates a new resource.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateResourceRequest request, CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateResourceCommand>();
        var result = await sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Updates an existing resource.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateResourceRequest request, CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateResourceCommand>() with { Id = id };
        var result = await sender.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Deletes a resource.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteResourceCommand(id), cancellationToken);
        return HandleResult(result);
    }
}
