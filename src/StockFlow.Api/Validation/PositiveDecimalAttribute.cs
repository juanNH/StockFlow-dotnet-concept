using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
internal sealed class PositiveDecimalAttribute : ValidationAttribute
{
    public PositiveDecimalAttribute()
    {
        ErrorMessage = "Unit price must be greater than zero.";
    }

    public override bool IsValid(object? value)
    {
        return value is decimal amount && amount > 0;
    }
}
