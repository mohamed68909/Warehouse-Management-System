using MediatR;
using WMSPro.Application.Abstractions;
using WMSPro.Application.Abstractions.Repositories;
using WMSPro.Application.Products.Commands;
using WMSPro.Domain.Common;
using WMSPro.Domain.Entities;

namespace WMSPro.Application.Products.Handlers;

// --- Helper mapper ---
internal static class ProductMapper
{
    public static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.SKU, p.Category, p.Price, p.Quantity, p.IsLowStock);
}

// ─── Get All ────────────────────────────────────────────────────────────────
public class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    private readonly IProductRepository _repo;
    public GetProductsHandler(IProductRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(
        GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _repo.GetAllAsync(request.Search, cancellationToken);

        return Result.Success<IReadOnlyList<ProductDto>>(
            products.Select(ProductMapper.ToDto).ToList().AsReadOnly());
    }
}

// ─── Get By Id ──────────────────────────────────────────────────────────────
public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IProductRepository _repo;
    public GetProductByIdHandler(IProductRepository repo) => _repo = repo;

    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDto>(Error.NotFound(nameof(Product), request.Id));

        return Result.Success(ProductMapper.ToDto(product));
    }
}

// ─── Create ─────────────────────────────────────────────────────────────────
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(IProductRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(
        CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.ExistsBySkuAsync(request.SKU, cancellationToken))
            return Result.Failure<ProductDto>(Error.Conflict($"A product with SKU '{request.SKU}' already exists."));

        var result = Product.Create(
            request.Name, request.SKU, request.Category, request.Price, request.Quantity);

        if (result.IsFailure)
            return Result.Failure<ProductDto>(result.Error);

        await _repo.AddAsync(result.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(result.Value));
    }
}

// ─── Update ─────────────────────────────────────────────────────────────────
public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IProductRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductHandler(IProductRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
            return Result.Failure(Error.NotFound(nameof(Product), request.Id));

        var updateResult = product.Update(request.Name, request.Category, request.Price);
        if (updateResult.IsFailure)
            return updateResult;

        _repo.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// ─── Delete ─────────────────────────────────────────────────────────────────
public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductHandler(IProductRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repo.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
            return Result.Failure(Error.NotFound(nameof(Product), request.Id));

        _repo.Delete(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
