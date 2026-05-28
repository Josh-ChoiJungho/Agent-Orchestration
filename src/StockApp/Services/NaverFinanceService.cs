using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StockApp.Models;

namespace StockApp.Services;

public class NaverFinanceService : IFinanceService
{
    private const string StockApiTemplate = "https://m.stock.naver.com/api/stock/{0}/basic";
    private const string IndexApiTemplate = "https://m.stock.naver.com/api/index/{0}/basic";
    private const string MarketIndicatorApiTemplate = "https://m.stock.naver.com/front-api/marketIndex/productDetail?category={0}&reutersCode={1}";

    private readonly HttpClient _httpClient;

    public NaverFinanceService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<FinanceQuote> GetIndexAsync(string indexCode, CancellationToken cancellationToken = default)
    {
        var url = string.Format(CultureInfo.InvariantCulture, IndexApiTemplate, indexCode);
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        return ParseIndexResponse(json);
    }

    public async Task<FinanceQuote> GetStockAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        var url = string.Format(CultureInfo.InvariantCulture, StockApiTemplate, stockCode);
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        return ParseStockResponse(json);
    }

    public async Task<FinanceQuote> GetExchangeRateAsync(string pairCode, CancellationToken cancellationToken = default)
    {
        var url = string.Format(CultureInfo.InvariantCulture, MarketIndicatorApiTemplate, "exchange", pairCode);
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        return ParseMarketIndicatorResponse(json, pairCode);
    }

    public async Task<FinanceQuote> GetGoldPriceAsync(CancellationToken cancellationToken = default)
    {
        const string goldCode = "CMDT_GD";
        var url = string.Format(CultureInfo.InvariantCulture, MarketIndicatorApiTemplate, "metals", goldCode);
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        return ParseMarketIndicatorResponse(json, goldCode);
    }

    internal FinanceQuote ParseStockResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var symbol = TryGetString(root, "itemCode") ?? string.Empty;
        var price = ReadDecimal(root, "closePrice");
        var change = ReadDecimal(root, "compareToPreviousClosePrice");
        var rate = ReadDecimal(root, "fluctuationsRatio");
        var sign = TryGetString(root, "compareToPreviousPriceCode");
        change = ApplySign(change, sign);
        rate = ApplySign(rate, sign);

        return new FinanceQuote(symbol, price, change, rate, "KRW");
    }

    internal FinanceQuote ParseIndexResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var symbol = TryGetString(root, "indexCode") ?? TryGetString(root, "code") ?? string.Empty;
        var value = ReadDecimal(root, "closePrice");
        var change = ReadDecimal(root, "compareToPreviousClosePrice");
        var rate = ReadDecimal(root, "fluctuationsRatio");
        var sign = TryGetString(root, "compareToPreviousPriceCode");
        change = ApplySign(change, sign);
        rate = ApplySign(rate, sign);

        return new FinanceQuote(symbol, value, change, rate, "P");
    }

    internal FinanceQuote ParseMarketIndicatorResponse(string json, string symbol)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Object)
        {
            root = result;
        }

        var value = ReadDecimal(root, "closePrice");
        if (value == 0m)
        {
            value = ReadDecimal(root, "calcPrice");
        }
        if (value == 0m)
        {
            value = ReadDecimal(root, "closeValue");
        }
        var change = ReadDecimal(root, "compareToPreviousClosePrice");
        if (change == 0m)
        {
            change = ReadDecimal(root, "compareToPreviousPrice");
        }
        var rate = ReadDecimal(root, "fluctuationsRatio");
        var sign = TryGetString(root, "compareToPreviousPriceCode");
        change = ApplySign(change, sign);
        rate = ApplySign(rate, sign);

        var unit = TryGetString(root, "currencyName") ?? TryGetString(root, "unit") ?? string.Empty;
        return new FinanceQuote(symbol, value, change, rate, unit);
    }

    private async Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private static decimal ReadDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return 0m;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out var n) ? n : 0m,
            JsonValueKind.String => decimal.TryParse(
                element.GetString()?.Replace(",", string.Empty, StringComparison.Ordinal),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var s) ? s : 0m,
            _ => 0m
        };
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }
        return null;
    }

    private static decimal ApplySign(decimal value, string? signCode)
    {
        if (value == 0m || string.IsNullOrEmpty(signCode))
        {
            return value;
        }

        var magnitude = Math.Abs(value);
        return signCode switch
        {
            "2" or "FALL" or "LOWER_LIMIT" => -magnitude,
            "5" or "RISE" or "UPPER_LIMIT" => magnitude,
            _ => value
        };
    }
}
