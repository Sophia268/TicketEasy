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
using System.Text.Json;

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

        // Extract raw code if needed
        string rawCode = codeInfo;
        if (isScan && codeInfo.Trim().StartsWith("{") && codeInfo.Contains("\"code\""))
        {
            try
            {
                // Simple extraction or use JSON parser. Let's try to extract "code" value.
                // Or if the backend expects the raw code, we need to extract it.
                // The prompt implies the QR is JSON: {"code":"xxxx",...}
                // But the API call {code} parameter expects just the code string.
                // So we MUST extract the code from the JSON.

                using (JsonDocument doc = JsonDocument.Parse(codeInfo))
                {
                    if (doc.RootElement.TryGetProperty("code", out JsonElement codeElement))
                    {
                        rawCode = codeElement.GetString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Warning: Failed to parse QR JSON: {ex.Message}. Using raw string.");
            }
        }
        else if (!isScan && codeInfo.Trim().StartsWith("{"))
        {
            // For ManualCheckAsync we constructed a JSON string:
            // $"{{\"code\":\"{ManualCode}\",...}}"
            // So we should also extract it, OR just use ManualCode directly in the first place.
            // But to keep consistent with the method signature, let's extract or better yet, fix the caller.
            // Actually, the caller ManualCheckAsync passes a JSON string.
            // Let's parse it back to get the code, or just rely on ManualCode property if available?
            // Since we have ManualCode property, let's just use rawCode = ManualCode if !isScan?
            // But let's be robust and parse if it looks like JSON.

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(codeInfo))
                {
                    if (doc.RootElement.TryGetProperty("code", out JsonElement codeElement))
                    {
                        rawCode = codeElement.GetString() ?? "";
                    }
                }
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(rawCode))
        {
            AddLog("Error: Could not extract code from input.");
            return;
        }

        var result = await _ticketService.ValidateTicketAsync(ProductId, rawCode);

        if (result == null)
        {
            ResultText = "Error";
            IsResultOk = false;
            AddLog("Network Error or Unknown Response");
            return;
        }

        if (result.Status == "ok")
        {
            if (result.Code == 200)
            {
                // Success
                ResultText = $"OK: {rawCode}";
                IsResultOk = true;
                AddLog($"Ticket Valid: {rawCode}");
                if (result.Msg?.CheckedAt != null)
                {
                    AddLog($"Checked At: {result.Msg.CheckedAt}");
                }
            }
            else if (result.Code == 600)
            {
                // Already Used
                ResultText = "USED";
                IsResultOk = false;
                AddLog($"Ticket Used! Code: {rawCode}");
            }
            else if (result.Code == 500)
            {
                // Expired
                ResultText = "EXPIRED";
                IsResultOk = false;
                AddLog($"Ticket Expired! Code: {rawCode}");
            }
            else
            {
                // Other OK status?
                ResultText = $"Status: {result.Code}";
                IsResultOk = false;
                AddLog($"Status {result.Code}: {result.Msg?.Message ?? "Unknown"}");
            }
        }
        else
        {
            // Error status
            ResultText = "Failure";
            IsResultOk = false;
            AddLog($"Error {result.Code}: {result.Msg?.Error ?? result.Msg?.Message ?? "Unknown Error"}");
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
        // 1. Try to use the injected platform-specific launcher (e.g., Android Intent)
        if (App.UrlLauncher != null)
        {
            App.UrlLauncher.Invoke(url);
            return;
        }

        // 2. Fallback to Process.Start for Desktop (Windows/Linux/macOS)
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
