using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Inventory;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Inventory.Warehouses;

[RequirePermission(Permissions.Inventory.TransferCreate)]
public record CreateWarehouseCommand(
    Guid CompanyId,
    Guid BranchId,
    string Code,
    string Name,
    string? Location) : IRequest<Result<Guid>>;

public class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateWarehouseCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateWarehouseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        var codeUpper = request.Code.Trim().ToUpperInvariant();
        var exists = await context.Warehouses
            .AnyAsync(w => w.CompanyId == request.CompanyId && w.Code == codeUpper, cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure(
                Error.Conflict("Warehouse.DuplicateCode", $"Gudang dengan kode '{codeUpper}' sudah terdaftar."));
        }

        var warehouse = Warehouse.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            code: codeUpper,
            name: request.Name,
            location: request.Location,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(warehouse.Id);
    }
}

[RequirePermission(Permissions.Inventory.TransferCreate)]
public record UpdateWarehouseCommand(
    Guid Id,
    string Name,
    string? Location,
    bool IsActive) : IRequest<Result>;

public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateWarehouseCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateWarehouseCommand, Result>
{
    public async Task<Result> Handle(
        UpdateWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        var warehouse = await context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);

        if (warehouse == null)
        {
            return Result.Failure(
                Error.NotFound("Warehouse.NotFound", "Data gudang tidak ditemukan."));
        }

        warehouse.Update(
            name: request.Name,
            location: request.Location,
            isActive: request.IsActive,
            updatedByUserId: currentUserService.UserId ?? Guid.Empty);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

