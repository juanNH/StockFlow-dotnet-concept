using Microsoft.AspNetCore.Identity;

namespace StockFlow.Infrastructure.Identity;

internal static class DummyPasswordVerifier
{
    private const string DummyPassword = "ThisIsNotARealAccountPassword9!";
    private static readonly PasswordHasher<StockFlowUser> Hasher = new();
    private static readonly StockFlowUser DummyUser = new();
    private static readonly string PasswordHash = Hasher.HashPassword(DummyUser, DummyPassword);

    public static void Verify(string password)
    {
        _ = Hasher.VerifyHashedPassword(DummyUser, PasswordHash, password);
    }
}
