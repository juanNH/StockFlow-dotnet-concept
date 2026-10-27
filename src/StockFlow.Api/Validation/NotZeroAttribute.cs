using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
internal sealed class NotZeroAttribute : ValidationAttribute
{
    public NotZeroAttribute()
    {
        ErrorMessage = "Quantity delta cannot be zero.";
    }

    public override bool IsValid(object? value)
    {
        return value is long quantity && quantity != 0;
    }
}
