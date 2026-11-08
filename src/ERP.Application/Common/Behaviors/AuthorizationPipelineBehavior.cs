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
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var permissionAttributes = request.GetType().GetCustomAttributes<RequirePermissionAttribute>().ToList();

        if (permissionAttributes.Count != 0)
        {
            if (!currentUserService.IsAuthenticated)
            {
                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(Error)]);
                    if (failureMethod != null)
                    {
                        return (TResponse)failureMethod.Invoke(null, [Error.Unauthorized("Auth.Unauthorized", "Pengguna belum terautentikasi.")])!;
                    }
                }
                else if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.Failure(Error.Unauthorized("Auth.Unauthorized", "Pengguna belum terautentikasi."));
                }

                throw new UnauthorizedAccessException("Pengguna belum terautentikasi.");
            }

            foreach (var attr in permissionAttributes)
            {
                if (!currentUserService.HasPermission(attr.PermissionCode))
                {
                    var msg = $"Akses ditolak: Anda tidak memiliki izin '{attr.PermissionCode}'.";
                    if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                    {
                        var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(Error)]);
                        if (failureMethod != null)
                        {
                            return (TResponse)failureMethod.Invoke(null, [Error.Forbidden("Auth.Forbidden", msg)])!;
                        }
                    }
                    else if (typeof(TResponse) == typeof(Result))
                    {
                        return (TResponse)(object)Result.Failure(Error.Forbidden("Auth.Forbidden", msg));
                    }

                    throw new UnauthorizedAccessException(msg);
                }
            }
        }

        return await next();
    }
}

