using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.CurrencyRates;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/v1/currency-rates")]
public sealed class CurrencyRatesController(GetCurrencyRateHandler handler) : ControllerBase
{
    [HttpGet("{baseCurrency}/{quoteCurrency}")]
    [ProducesResponseType(typeof(CurrencyRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CurrencyRateResponse>> GetRate(
        [RegularExpression("^[A-Za-z]{3}$")] string baseCurrency,
        [RegularExpression("^[A-Za-z]{3}$")] string quoteCurrency,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(baseCurrency, quoteCurrency, cancellationToken);
        return Ok(result);
    }
}
