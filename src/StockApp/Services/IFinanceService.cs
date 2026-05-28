using System.Threading;
using System.Threading.Tasks;
using StockApp.Models;

namespace StockApp.Services;

public record FinanceQuote(
    string Symbol,
    decimal CurrentValue,
    decimal ChangeValue,
    decimal ChangeRate,
    string? Unit = null);

public interface IFinanceService
{
    Task<FinanceQuote> GetIndexAsync(string indexCode, CancellationToken cancellationToken = default);

    Task<FinanceQuote> GetStockAsync(string stockCode, CancellationToken cancellationToken = default);

    Task<FinanceQuote> GetExchangeRateAsync(string pairCode, CancellationToken cancellationToken = default);

    Task<FinanceQuote> GetGoldPriceAsync(CancellationToken cancellationToken = default);

    FinanceQuote ParseStockResponse(string json);

    FinanceQuote ParseIndexResponse(string json);

    FinanceQuote ParseMarketIndicatorResponse(string json, string symbol);
}
