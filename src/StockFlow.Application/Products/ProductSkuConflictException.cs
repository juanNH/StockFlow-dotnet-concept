namespace StockFlow.Application.Products;

public sealed class ProductSkuConflictException() : Exception("A product with this SKU already exists in the current organization.");
