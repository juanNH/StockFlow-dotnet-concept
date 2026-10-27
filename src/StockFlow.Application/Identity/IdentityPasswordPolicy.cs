namespace StockFlow.Application.Identity;

public static class IdentityPasswordPolicy
{
    public const int MinimumLength = 12;

    public const int MaximumLength = 128;

    public const string Pattern = "^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])[\\s\\S]{12,128}$";
}
