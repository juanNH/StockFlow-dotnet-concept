namespace StockFlow.Application.Products;

public sealed class ProductNotFoundException() : Exception("The product was not found in the current organization.");
