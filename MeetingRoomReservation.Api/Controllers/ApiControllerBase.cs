using MeetingRoomReservation.Domain.Results;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomReservation.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult(Result result) =>
        result.IsSuccess ? NoContent() : MapError(result.Error!);

    protected IActionResult HandleResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);

    protected ObjectResult MapError(DomainError error) => error switch
    {
        ValidationError => Problem(detail: error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed"),
        NotFoundError => Problem(detail: error.Message, statusCode: StatusCodes.Status404NotFound, title: "Not found"),
        ConflictError => Problem(detail: error.Message, statusCode: StatusCodes.Status409Conflict, title: "Conflict"),
        _ => Problem(detail: error.Message, statusCode: StatusCodes.Status500InternalServerError),
    };
}
