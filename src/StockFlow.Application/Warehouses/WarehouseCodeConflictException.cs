namespace StockFlow.Application.Warehouses;

public sealed class WarehouseCodeConflictException() : Exception("A warehouse with this code already exists in the current organization.");
