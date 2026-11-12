using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.Domain.Exceptions;
using ERP.Shared;

namespace ERP.Application.Common.Behaviors;

public class ExceptionHandlingPipelineBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (DomainException domainEx)
        {
            logger.LogWarning(domainEx, "Domain rule violation while processing {RequestName}: {Message}", typeof(TRequest).Name, domainEx.Message);
            return CreateFailureResponse(Error.Conflict("Domain.RuleViolation", domainEx.Message));
        }
        catch (DbUpdateConcurrencyException concurrencyEx)
        {
            logger.LogWarning(concurrencyEx, "Database concurrency conflict while processing {RequestName}", typeof(TRequest).Name);
            return CreateFailureResponse(Error.Conflict(
                "Database.ConcurrencyConflict",
                "Data telah diperbarui oleh pengguna lain secara bersamaan. Silakan muat ulang halaman dan coba kembali."));
        }
        catch (InvalidOperationException invEx) when (invEx.Message.Contains("BR-", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(invEx, "Business rule violation while processing {RequestName}: {Message}", typeof(TRequest).Name, invEx.Message);
            return CreateFailureResponse(Error.Conflict("Domain.BusinessRuleViolation", invEx.Message));
        }
    }

    private static TResponse CreateFailureResponse(Error error)
    {
        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(Error)]);
            if (failureMethod != null)
            {
                return (TResponse)failureMethod.Invoke(null, [error])!;
            }
        }
        else if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        throw new InvalidOperationException(error.Message);
    }
}
