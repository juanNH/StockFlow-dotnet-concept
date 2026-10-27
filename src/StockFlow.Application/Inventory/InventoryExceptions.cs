namespace StockFlow.Application.Inventory;

public sealed class InsufficientStockException() : Exception("The adjustment would result in a negative stock balance.");

public sealed class StockQuantityOutOfRangeException() : Exception("The resulting stock quantity is outside the supported range.");
