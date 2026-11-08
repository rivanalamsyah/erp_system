using FluentValidation;
using MediatR;
using ERP.Shared;

namespace ERP.Application.Common.Behaviors;

public class ValidationPipelineBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            var errorMessage = string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));
            
            // Jika TResponse bertipe Result atau Result<T>, kembalikan Failure result langsung
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(Error)]);
                if (failureMethod != null)
                {
                    var error = Error.Validation("Validation.Failed", errorMessage);
                    return (TResponse)failureMethod.Invoke(null, [error])!;
                }
            }
            else if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(Error.Validation("Validation.Failed", errorMessage));
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}

