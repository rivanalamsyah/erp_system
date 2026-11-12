using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Hr;
using ERP.Shared;
using ERP.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Leaves;

[RequirePermission(Permissions.Hr.LeaveApprove)]
public record ApproveLeaveRequestCommand(Guid LeaveRequestId, bool Approve) : IRequest<Result<Unit>>;

public class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ApproveLeaveRequestCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leave = await _context.LeaveRequests
            .FirstOrDefaultAsync(l => l.Id == request.LeaveRequestId, cancellationToken);

        if (leave == null)
        {
            return Result<Unit>.Failure(Error.NotFound("Leave.NotFound", "Pengajuan cuti tidak ditemukan."));
        }

        if (leave.Status != LeaveRequestStatus.Pending)
        {
            return Result<Unit>.Failure(Error.Conflict("Leave.AlreadyProcessed", "Pengajuan cuti sudah diproses sebelumnya."));
        }

        var userId = _currentUserService.UserId ?? Guid.NewGuid();

        if (request.Approve)
        {
            leave.Approve(userId);
        }
        else
        {
            leave.Reject(userId);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<Unit>.Success(Unit.Value);
    }
}
