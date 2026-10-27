namespace StockFlow.Application.Identity;

public sealed class IdentityConflictException()
    : Exception("The email address or organization slug is already registered.");
