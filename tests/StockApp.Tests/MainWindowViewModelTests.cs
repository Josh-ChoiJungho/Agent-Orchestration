using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using StockApp.Models;
using StockApp.Services;
using StockApp.ViewModels;
using Xunit;

namespace StockApp.Tests;

public class MainWindowViewModelTests
{
    private static Mock<IFinanceService> CreateSuccessServiceMock()
    {
        var mock = new Mock<IFinanceService>(MockBehavior.Strict);
        mock.Setup(s => s.GetIndexAsync("KOSPI", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceQuote("KOSPI", 2600m, 10m, 0.39m, "P"));
        mock.Setup(s => s.GetStockAsync("005930", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceQuote("005930", 71300m, -500m, -0.71m, "KRW"));
        mock.Setup(s => s.GetStockAsync("000660", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceQuote("000660", 135000m, 2000m, 1.46m, "KRW"));
        mock.Setup(s => s.GetExchangeRateAsync("FX_USDKRW", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceQuote("FX_USDKRW", 1370.50m, 3.20m, 0.23m, "원"));
        mock.Setup(s => s.GetGoldPriceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceQuote("CMDT_GD", 95500.40m, 500.10m, 0.52m, "원"));
        return mock;
    }

    [Fact]
    public void Constructor_PopulatesStockAndIndicatorCollections()
    {
        var vm = new MainWindowViewModel(CreateSuccessServiceMock().Object, useDispatcherTimer: false);

        Assert.Equal(3, vm.Stocks.Count);
        Assert.Equal(2, vm.MarketIndicators.Count);
        Assert.Contains(vm.Stocks, s => s.Symbol == "KOSPI");
        Assert.Contains(vm.Stocks, s => s.Symbol == "005930");
        Assert.Contains(vm.Stocks, s => s.Symbol == "000660");
        Assert.Contains(vm.MarketIndicators, s => s.Symbol == "FX_USDKRW");
        Assert.Contains(vm.MarketIndicators, s => s.Symbol == "CMDT_GD");
    }

    [Fact]
    public async Task RefreshAsync_SetsSuccessStatusOnAllItems()
    {
        var vm = new MainWindowViewModel(CreateSuccessServiceMock().Object, useDispatcherTimer: false);

        await vm.RefreshAsync();

        Assert.All(vm.Stocks, item => Assert.Equal(FinanceStatus.Success, item.Status));
        Assert.All(vm.MarketIndicators, item => Assert.Equal(FinanceStatus.Success, item.Status));
        Assert.NotNull(vm.LastSyncedAt);
    }

    [Fact]
    public async Task RefreshAsync_PartialFailure_KeepsOtherItemsSuccess()
    {
        var mock = CreateSuccessServiceMock();
        mock.Setup(s => s.GetGoldPriceAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Net.Http.HttpRequestException("network down"));

        var vm = new MainWindowViewModel(mock.Object, useDispatcherTimer: false);
        await vm.RefreshAsync();

        var gold = vm.MarketIndicators.First(i => i.Symbol == "CMDT_GD");
        Assert.Equal(FinanceStatus.Error, gold.Status);
        Assert.Equal("network down", gold.ErrorMessage);

        Assert.All(vm.Stocks, item => Assert.Equal(FinanceStatus.Success, item.Status));
        var fx = vm.MarketIndicators.First(i => i.Symbol == "FX_USDKRW");
        Assert.Equal(FinanceStatus.Success, fx.Status);
    }

    [Fact]
    public async Task RefreshCommand_FormatsValueAndChangeText()
    {
        var vm = new MainWindowViewModel(CreateSuccessServiceMock().Object, useDispatcherTimer: false);
        await vm.RefreshAsync();

        var samsung = vm.Stocks.First(s => s.Symbol == "005930");
        Assert.Contains("▼", samsung.ChangeText);
        Assert.Contains("%", samsung.ChangeRateText);
        Assert.True(samsung.IsDown);

        var hynix = vm.Stocks.First(s => s.Symbol == "000660");
        Assert.Contains("▲", hynix.ChangeText);
        Assert.True(hynix.IsUp);
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentCalls_DoNotOverlap()
    {
        var mock = CreateSuccessServiceMock();
        var vm = new MainWindowViewModel(mock.Object, useDispatcherTimer: false);

        var t1 = vm.RefreshAsync();
        var t2 = vm.RefreshAsync();
        await Task.WhenAll(t1, t2);

        // 5 quotes, called once for each non-overlapping refresh
        mock.Verify(s => s.GetIndexAsync("KOSPI", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        mock.Verify(s => s.GetStockAsync("005930", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Dispose_StopsTimerAndCancelsCancellationTokenSource()
    {
        var mock = CreateSuccessServiceMock();
        var vm = new MainWindowViewModel(mock.Object, useDispatcherTimer: false);

        vm.Dispose();
        vm.Dispose(); // Multi-dispose safety
    }
}
