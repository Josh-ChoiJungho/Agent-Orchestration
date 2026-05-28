using System;
using Prism.Mvvm;

namespace StockApp.Models;

public enum FinanceStatus
{
    Idle,
    Loading,
    Success,
    Error
}

public class FinanceItem : BindableBase
{
    private string _displayName = string.Empty;
    private string _symbol = string.Empty;
    private string _valueText = "-";
    private string _changeText = "-";
    private string _changeRateText = "-";
    private decimal _changeValue;
    private DateTime? _updatedAt;
    private FinanceStatus _status = FinanceStatus.Idle;
    private string? _errorMessage;
    private string _unit = string.Empty;

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string Symbol
    {
        get => _symbol;
        set => SetProperty(ref _symbol, value);
    }

    public string ValueText
    {
        get => _valueText;
        set => SetProperty(ref _valueText, value);
    }

    public string ChangeText
    {
        get => _changeText;
        set => SetProperty(ref _changeText, value);
    }

    public string ChangeRateText
    {
        get => _changeRateText;
        set => SetProperty(ref _changeRateText, value);
    }

    public decimal ChangeValue
    {
        get => _changeValue;
        set
        {
            if (SetProperty(ref _changeValue, value))
            {
                RaisePropertyChanged(nameof(IsUp));
                RaisePropertyChanged(nameof(IsDown));
            }
        }
    }

    public bool IsUp => _changeValue > 0;
    public bool IsDown => _changeValue < 0;

    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set
        {
            if (SetProperty(ref _updatedAt, value))
            {
                RaisePropertyChanged(nameof(UpdatedAtText));
            }
        }
    }

    public string UpdatedAtText => _updatedAt.HasValue
        ? _updatedAt.Value.ToString("HH:mm:ss")
        : "-";

    public FinanceStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                RaisePropertyChanged(nameof(StatusText));
            }
        }
    }

    public string StatusText => _status switch
    {
        FinanceStatus.Idle => "대기",
        FinanceStatus.Loading => "조회 중",
        FinanceStatus.Success => "정상",
        FinanceStatus.Error => "조회 실패",
        _ => string.Empty
    };

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }
}
