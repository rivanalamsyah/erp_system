using System.Collections.Concurrent;
using System.Reflection;
using MediatR;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;

namespace ERP.Application.Common.Behaviors;

public class AuthorizationPipelineBehavior<TRequest, TResponse>(ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, RequirePermissionAttribute[]> PermissionCache = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var permissionAttributes = PermissionCache.GetOrAdd(
            request.GetType(),
            static type => type.GetCustomAttributes<RequirePermissionAttribute>(inherit: true).ToArray());

        if (!currentUserService.IsAuthenticated)
        {
            return CreateFailureResponse(Error.Unauthorized("Auth.Unauthorized", "Pengguna belum terautentikasi."));
        }

        if (permissionAttributes.Length != 0)
        {
            foreach (var attr in permissionAttributes)
            {
                if (!currentUserService.HasPermission(attr.PermissionCode))
                {
                    var msg = $"Akses ditolak: Anda tidak memiliki izin '{attr.PermissionCode}'.";
                    return CreateFailureResponse(Error.Forbidden("Auth.Forbidden", msg));
                }
            }
        }

        return await next();
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

        throw new UnauthorizedAccessException(error.Message);
    }
}
