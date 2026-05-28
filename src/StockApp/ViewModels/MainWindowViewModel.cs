using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using StockApp.Models;
using StockApp.Services;

namespace StockApp.ViewModels;

public class MainWindowViewModel : BindableBase, IDisposable
{
    private const string KospiSymbol = "KOSPI";
    private const string SamsungSymbol = "005930";
    private const string HynixSymbol = "000660";
    private const string UsdKrwSymbol = "FX_USDKRW";
    private const string GoldSymbol = "CMDT_GC";

    private readonly IFinanceService _financeService;
    private readonly DispatcherTimer? _timer;
    private readonly object _refreshLock = new();
    private bool _isRefreshing;
    private bool _disposed;
    private DateTime? _lastSyncedAt;
    private string _statusMessage = "초기 데이터 조회 중...";

    public MainWindowViewModel(IFinanceService financeService)
        : this(financeService, useDispatcherTimer: true)
    {
    }

    internal MainWindowViewModel(IFinanceService financeService, bool useDispatcherTimer)
    {
        _financeService = financeService ?? throw new ArgumentNullException(nameof(financeService));

        Stocks = new ObservableCollection<FinanceItem>
        {
            new() { DisplayName = "코스피", Symbol = KospiSymbol, Unit = "P" },
            new() { DisplayName = "삼성전자", Symbol = SamsungSymbol, Unit = "원" },
            new() { DisplayName = "SK하이닉스", Symbol = HynixSymbol, Unit = "원" }
        };

        MarketIndicators = new ObservableCollection<FinanceItem>
        {
            new() { DisplayName = "환율 (USD/KRW)", Symbol = UsdKrwSymbol, Unit = "원" },
            new() { DisplayName = "금시세", Symbol = GoldSymbol, Unit = "USD/oz" }
        };

        RefreshCommand = new DelegateCommand(async () => await RefreshAsync().ConfigureAwait(false))
            .ObservesCanExecute(() => CanRefresh);

        if (useDispatcherTimer && System.Windows.Application.Current is not null)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
            _ = RefreshAsync();
        }
    }

    public ObservableCollection<FinanceItem> Stocks { get; }

    public ObservableCollection<FinanceItem> MarketIndicators { get; }

    public DelegateCommand RefreshCommand { get; }

    public bool CanRefresh => !_isRefreshing;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public DateTime? LastSyncedAt
    {
        get => _lastSyncedAt;
        private set
        {
            if (SetProperty(ref _lastSyncedAt, value))
            {
                RaisePropertyChanged(nameof(LastSyncedText));
            }
        }
    }

    public string LastSyncedText => _lastSyncedAt.HasValue
        ? _lastSyncedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
        : "-";

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        lock (_refreshLock)
        {
            if (_isRefreshing)
            {
                return;
            }
            _isRefreshing = true;
        }
        RaisePropertyChanged(nameof(CanRefresh));
        RefreshCommand.RaiseCanExecuteChanged();

        try
        {
            MarkLoading();
            StatusMessage = "시세 조회 중...";

            var kospiTask = SafeFetchAsync(Stocks[0], () => _financeService.GetIndexAsync(KospiSymbol, cancellationToken));
            var samsungTask = SafeFetchAsync(Stocks[1], () => _financeService.GetStockAsync(SamsungSymbol, cancellationToken));
            var hynixTask = SafeFetchAsync(Stocks[2], () => _financeService.GetStockAsync(HynixSymbol, cancellationToken));
            var fxTask = SafeFetchAsync(MarketIndicators[0], () => _financeService.GetExchangeRateAsync(UsdKrwSymbol, cancellationToken));
            var goldTask = SafeFetchAsync(MarketIndicators[1], () => _financeService.GetGoldPriceAsync(cancellationToken));

            await Task.WhenAll(kospiTask, samsungTask, hynixTask, fxTask, goldTask).ConfigureAwait(false);

            LastSyncedAt = DateTime.Now;
            StatusMessage = "최근 갱신 완료";
        }
        finally
        {
            lock (_refreshLock)
            {
                _isRefreshing = false;
            }
            RaisePropertyChanged(nameof(CanRefresh));
            RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task SafeFetchAsync(FinanceItem item, Func<Task<FinanceQuote>> fetch)
    {
        try
        {
            var quote = await fetch().ConfigureAwait(false);
            ApplyQuote(item, quote);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            item.Status = FinanceStatus.Error;
            item.ErrorMessage = ex.Message;
        }
    }

    private static void ApplyQuote(FinanceItem item, FinanceQuote quote)
    {
        item.ValueText = FormatValue(quote.CurrentValue);
        item.ChangeText = FormatChange(quote.ChangeValue);
        item.ChangeRateText = FormatRate(quote.ChangeRate);
        item.ChangeValue = quote.ChangeValue;
        item.UpdatedAt = DateTime.Now;
        item.Status = FinanceStatus.Success;
        item.ErrorMessage = null;
        if (!string.IsNullOrWhiteSpace(quote.Unit))
        {
            item.Unit = quote.Unit!;
        }
    }

    private static string FormatValue(decimal value) =>
        value.ToString("N2", CultureInfo.GetCultureInfo("ko-KR"));

    private static string FormatChange(decimal value)
    {
        var sign = value > 0 ? "▲" : value < 0 ? "▼" : "-";
        return $"{sign} {Math.Abs(value).ToString("N2", CultureInfo.GetCultureInfo("ko-KR"))}";
    }

    private static string FormatRate(decimal rate)
    {
        var sign = rate > 0 ? "+" : string.Empty;
        return $"{sign}{rate.ToString("0.00", CultureInfo.InvariantCulture)}%";
    }

    private void MarkLoading()
    {
        foreach (var item in Stocks)
        {
            if (item.Status != FinanceStatus.Success)
            {
                item.Status = FinanceStatus.Loading;
            }
        }
        foreach (var item in MarketIndicators)
        {
            if (item.Status != FinanceStatus.Success)
            {
                item.Status = FinanceStatus.Loading;
            }
        }
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            await RefreshAsync().ConfigureAwait(false);
        }
        catch
        {
            // 타이머 콜백은 앱을 종료시키지 않는다. 개별 항목의 오류는 SafeFetchAsync에서 흡수된다.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
        }
        GC.SuppressFinalize(this);
    }
}
