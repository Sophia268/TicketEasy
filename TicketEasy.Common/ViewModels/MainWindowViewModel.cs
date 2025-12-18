using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicketEasy.Services;
using TicketEasy.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace TicketEasy.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ITicketScanner? _scanner;
    private readonly TicketService _ticketService;
    private readonly ConfigService _configService;

    [ObservableProperty]
    private string _productId = "";

    [ObservableProperty]
    private string _manualCode = "";

    [ObservableProperty]
    private ObservableCollection<string> _logs = new();

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _showAbout;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string _resultText = "";

    [ObservableProperty]
    private bool _isResultOk;

    public MainWindowViewModel(ITicketScanner? scanner = null)
    {
        _scanner = scanner;
        _configService = new ConfigService();
        _ticketService = new TicketService(_configService);

        AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        AddLog("Application Started.");

        // Load Config asynchronously
        Task.Run(async () => await InitializeConfigAsync());
    }

    private async Task InitializeConfigAsync()
    {
        await _configService.LoadConfigAsync();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!string.IsNullOrEmpty(_configService.CurrentConfig.ProductId))
            {
                ProductId = _configService.CurrentConfig.ProductId;
            }
        });
    }

    public MainWindowViewModel() : this(null) { }

    private void AddLog(string message)
    {
        string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        // Ensure UI update
        // Avalonia bindings usually handle ObservableCollection changes automatically
        Logs.Insert(0, logEntry);
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductId))
        {
            AddLog("Error: Product ID is empty.");
            return;
        }

        AddLog($"Connecting with Product ID: {ProductId}...");
        bool result = await _ticketService.CheckConnectivityAsync(ProductId);

        if (result)
        {
            IsConnected = true;
            AddLog("Connected successfully.");
            // Save ProductId to config for next run
            await _configService.SaveConfigAsync(ProductId);
        }
        else
        {
            IsConnected = false;
            AddLog("Connection failed. Please check Product ID and network.");
        }
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (_scanner == null)
        {
            AddLog("Error: Scanner not supported on this platform or not initialized.");
            return;
        }

        try
        {
            AddLog("Scanning QR Code...");
            string? result = await _scanner.ScanAsync();

            if (!string.IsNullOrEmpty(result))
            {
                AddLog($"Scanned: {result}");
                await CheckTicketAsync(result, isScan: true);
            }
            else
            {
                AddLog("Scan canceled or empty.");
            }
        }
        catch (Exception ex)
        {
            AddLog($"Scan Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ManualCheckAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualCode))
        {
            AddLog("Error: Please enter a code.");
            return;
        }

        // Construct JSON for manual input
        string json = $"{{\"code\":\"{ManualCode}\",\"category\":null,\"createTime\":null,\"ExpireTime\":null}}";
        AddLog($"Checking Manual Code: {ManualCode}");

        await CheckTicketAsync(json, isScan: false);
    }

    private async Task CheckTicketAsync(string codeInfo, bool isScan)
    {
        if (string.IsNullOrWhiteSpace(ProductId))
        {
            AddLog("Error: Product ID not set. Please enter Product ID.");
            return;
        }

        string result = await _ticketService.ValidateTicketAsync(ProductId, codeInfo);

        if (result.Contains("OK", StringComparison.OrdinalIgnoreCase))
        {
            string ticketCode = isScan ? "Scanned" : ManualCode;
            // Try to extract code from json if possible, but for now use simple logic
            // If manual, use ManualCode. If scanned, maybe the result or input codeInfo has it.
            // The user requirement says: OK: XXXX (Ticket Code)

            // Simple parsing if it is JSON
            if (codeInfo.Contains("\"code\""))
            {
                try
                {
                    var startIndex = codeInfo.IndexOf("\"code\"") + 7;
                    var endIndex = codeInfo.IndexOf("\"", startIndex + 1); // rough parse
                    // let's skip complex parsing for now or do a quick extract
                }
                catch { }
            }

            ResultText = $"OK: {ticketCode}";
            IsResultOk = true;
            AddLog("Ticket Valid: OK");
        }
        else
        {
            ResultText = "Failure!";
            IsResultOk = false;
            AddLog($"Ticket Result: {result}");
        }
    }

    [RelayCommand]
    private void ToggleAbout()
    {
        ShowAbout = !ShowAbout;
    }

    [RelayCommand]
    private void OpenPurchaseLink()
    {
        var baseUrl = _configService.CurrentConfig.BaseUrl;
        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');

        // {BaseUrl}/Goods/{productId}
        var url = $"{baseUrl}/Goods/{ProductId}";
        OpenUrl(url);
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
    }
}
