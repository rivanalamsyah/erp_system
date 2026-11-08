using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Master;
using ERP.Shared.Common;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Products;

[RequirePermission(Permissions.Master.ProductManage)]
public record CreateProductCommand(
    Guid CompanyId,
    Guid CategoryId,
    Guid UomId,
    string Sku,
    string? Barcode,
    string Name,
    string? Description,
    ProductType ProductType,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal MinimumStockLevel = 0) : IRequest<Result<Guid>>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Kategori produk wajib dipilih.");
        RuleFor(x => x.UomId).NotEmpty().WithMessage("Satuan ukuran (UoM) wajib dipilih.");
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0).WithMessage("Harga beli tidak boleh negatif.");
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0).WithMessage("Harga jual tidak boleh negatif.");
        RuleFor(x => x.MinimumStockLevel).GreaterThanOrEqualTo(0).WithMessage("Minimum stok tidak boleh negatif.");
    }
}

public class CreateProductCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var skuUpper = request.Sku.Trim().ToUpperInvariant();
        var exists = await context.Products
            .AnyAsync(p => p.CompanyId == request.CompanyId && p.Sku == skuUpper, cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure(
                Error.Conflict("Product.DuplicateSku", $"Produk dengan SKU '{skuUpper}' sudah terdaftar."));
        }

        var product = Product.Create(
            companyId: request.CompanyId,
            categoryId: request.CategoryId,
            uomId: request.UomId,
            sku: skuUpper,
            barcode: request.Barcode,
            name: request.Name,
            description: request.Description,
            productType: request.ProductType,
            purchasePrice: request.PurchasePrice,
            sellingPrice: request.SellingPrice,
            minimumStockLevel: request.MinimumStockLevel,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }
}

[RequirePermission(Permissions.Master.ProductManage)]
public record UpdateProductCommand(
    Guid Id,
    Guid CategoryId,
    Guid UomId,
    string? Barcode,
    string Name,
    string? Description,
    ProductType ProductType,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal MinimumStockLevel,
    bool IsActive) : IRequest<Result>;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.UomId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStockLevel).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateProductCommand, Result>
{
    public async Task<Result> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product == null)
        {
            return Result.Failure(
                Error.NotFound("Product.NotFound", "Data produk tidak ditemukan."));
        }

        product.Update(
            categoryId: request.CategoryId,
            uomId: request.UomId,
            barcode: request.Barcode,
            name: request.Name,
            description: request.Description,
            productType: request.ProductType,
            purchasePrice: request.PurchasePrice,
            sellingPrice: request.SellingPrice,
            minimumStockLevel: request.MinimumStockLevel,
            isActive: request.IsActive,
            updatedByUserId: currentUserService.UserId ?? Guid.Empty);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

[RequirePermission(Permissions.Master.ProductManage)]
public record DeleteProductCommand(Guid Id) : IRequest<Result>;

public class DeleteProductCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product == null)
        {
            return Result.Failure(
                Error.NotFound("Product.NotFound", "Data produk tidak ditemukan."));
        }

        product.SoftDelete(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

