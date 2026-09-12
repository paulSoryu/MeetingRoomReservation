using FluentValidation;
using MediatR;
using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Application.Validation;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(validator => validator.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var message = string.Join(" ", failures.Select(failure => failure.ErrorMessage));
        var error = new ValidationError("Validation.Failed", message);

        return CreateFailureResponse(error);
    }

    // TResponse is Result or Result<T> - the closed generic factory to call is picked at runtime
    // since there's no way to express "TResponse : Result<T> for some T" as a generic constraint.
    private static TResponse CreateFailureResponse(ValidationError error)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(DomainError)])!
            .MakeGenericMethod(valueType);

        return (TResponse)failureMethod.Invoke(null, [error])!;
    }
}
