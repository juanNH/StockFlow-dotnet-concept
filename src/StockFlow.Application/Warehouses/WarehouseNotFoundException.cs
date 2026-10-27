namespace StockFlow.Application.Warehouses;

public sealed class WarehouseNotFoundException() : Exception("The warehouse was not found in the current organization.");
