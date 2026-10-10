using MediatR;
using WMSPro.Domain.Common;

namespace WMSPro.Application.Products.Commands;

// --- DTOs ---
public record ProductDto(
    Guid Id,
    string Name,
    string SKU,
    string Category,
    decimal Price,
    int Quantity,
    bool IsLowStock);

// --- Commands ---
public record CreateProductCommand(
    string Name,
    string SKU,
    string Category,
    decimal Price,
    int Quantity) : IRequest<Result<ProductDto>>;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Category,
    decimal Price) : IRequest<Result>;

public record DeleteProductCommand(Guid Id) : IRequest<Result>;

// --- Queries ---
public record GetProductsQuery(string? Search = null) : IRequest<Result<IReadOnlyList<ProductDto>>>;

public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>;

public record GetProductBySkuQuery(string Sku) : IRequest<Result<ProductDto>>;
