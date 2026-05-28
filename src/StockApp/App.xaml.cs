using System.Net.Http;
using System.Windows;
using Prism.Ioc;
using StockApp.Services;
using StockApp.ViewModels;
using StockApp.Views;

namespace StockApp;

public partial class App
{
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<HttpClient>(CreateHttpClient);
        containerRegistry.RegisterSingleton<IFinanceService, NaverFinanceService>();
        containerRegistry.RegisterSingleton<MainWindowViewModel>();
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = System.TimeSpan.FromSeconds(8)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
        client.DefaultRequestHeaders.Referrer = new System.Uri("https://m.stock.naver.com/");
        return client;
    }
}
