using System.Net.Http;
using StockApp.Services;
using Xunit;

namespace StockApp.Tests;

public class NaverFinanceServiceTests
{
    private static NaverFinanceService CreateService() => new(new HttpClient());

    [Fact]
    public void ParseStockResponse_ReturnsExpectedQuote()
    {
        const string json = """
        {
            "itemCode": "005930",
            "stockName": "삼성전자",
            "closePrice": "71,300",
            "compareToPreviousClosePrice": "500",
            "compareToPreviousPriceCode": "5",
            "fluctuationsRatio": "0.71"
        }
        """;

        var quote = CreateService().ParseStockResponse(json);

        Assert.Equal("005930", quote.Symbol);
        Assert.Equal(71300m, quote.CurrentValue);
        Assert.Equal(500m, quote.ChangeValue);
        Assert.Equal(0.71m, quote.ChangeRate);
    }

    [Fact]
    public void ParseStockResponse_NegativeSign_AppliesDownward()
    {
        const string json = """
        {
            "itemCode": "000660",
            "closePrice": "135000",
            "compareToPreviousClosePrice": "2000",
            "compareToPreviousPriceCode": "2",
            "fluctuationsRatio": "1.46"
        }
        """;

        var quote = CreateService().ParseStockResponse(json);

        Assert.Equal(-2000m, quote.ChangeValue);
        Assert.Equal(-1.46m, quote.ChangeRate);
    }

    [Fact]
    public void ParseIndexResponse_ReadsClosePrice()
    {
        const string json = """
        {
            "indexCode": "KOSPI",
            "closePrice": "2,600.50",
            "compareToPreviousClosePrice": "10.25",
            "compareToPreviousPriceCode": "5",
            "fluctuationsRatio": "0.39"
        }
        """;

        var quote = CreateService().ParseIndexResponse(json);

        Assert.Equal("KOSPI", quote.Symbol);
        Assert.Equal(2600.50m, quote.CurrentValue);
        Assert.Equal(10.25m, quote.ChangeValue);
        Assert.Equal(0.39m, quote.ChangeRate);
    }

    [Fact]
    public void ParseMarketIndicatorResponse_PreservesSymbolAndValues()
    {
        const string json = """
        {
            "closePrice": "1370.50",
            "compareToPreviousClosePrice": "3.20",
            "compareToPreviousPriceCode": "2",
            "fluctuationsRatio": "0.23",
            "currencyName": "원"
        }
        """;

        var quote = CreateService().ParseMarketIndicatorResponse(json, "FX_USDKRW");

        Assert.Equal("FX_USDKRW", quote.Symbol);
        Assert.Equal(1370.50m, quote.CurrentValue);
        Assert.Equal(-3.20m, quote.ChangeValue);
        Assert.Equal(-0.23m, quote.ChangeRate);
        Assert.Equal("원", quote.Unit);
    }

    [Fact]
    public void ParseStockResponse_MissingFields_DefaultsToZero()
    {
        const string json = """
        {
            "itemCode": "005930"
        }
        """;

        var quote = CreateService().ParseStockResponse(json);

        Assert.Equal("005930", quote.Symbol);
        Assert.Equal(0m, quote.CurrentValue);
        Assert.Equal(0m, quote.ChangeValue);
        Assert.Equal(0m, quote.ChangeRate);
    }

    [Fact]
    public void ParseMarketIndicatorResponse_GoldGD_ReadsCloseValueAndCompareToPreviousPrice()
    {
        const string json = """
        {
            "closeValue": "95500.00",
            "compareToPreviousPrice": "500.00",
            "compareToPreviousPriceCode": "5",
            "fluctuationsRatio": "0.52",
            "unit": "원"
        }
        """;

        var quote = CreateService().ParseMarketIndicatorResponse(json, "CMDT_GD");

        Assert.Equal("CMDT_GD", quote.Symbol);
        Assert.Equal(95500.00m, quote.CurrentValue);
        Assert.Equal(500.00m, quote.ChangeValue);
        Assert.Equal(0.52m, quote.ChangeRate);
        Assert.Equal("원", quote.Unit);
    }
}
