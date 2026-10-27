namespace StockFlow.Application.Identity;

public sealed class IdentityValidationException()
    : Exception("The registration details do not meet the account policy.");
