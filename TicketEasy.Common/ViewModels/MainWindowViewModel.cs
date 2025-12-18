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
        Dispatcher.UIThread.InvokeAsync(() => Logs.Insert(0, logEntry));
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
        var response = await _ticketService.CheckConnectivityAsync(ProductId);

        if (response != null && response.Status == "ok" && response.Code == 200)
        {
            IsConnected = true;
            AddLog("Connected successfully.");

            string hash = response.Data?.ProductHashcode ?? "";
            if (!string.IsNullOrEmpty(hash))
            {
                AddLog($"Got Product Hash: {hash}");
            }

            // Save ProductId and HashCode to config for next run
            await _configService.SaveConfigAsync(ProductId, hash);
        }
        else
        {
            IsConnected = false;
            string errMsg = response?.Msg ?? "Unknown Error";
            AddLog($"Connection failed. Code: {response?.Code}. Msg: {errMsg}");
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
    private async Task ScanImageAsync()
    {
        if (_scanner == null)
        {
            AddLog("Error: Scanner not supported on this platform or not initialized.");
            return;
        }

        try
        {
            AddLog("Picking Image...");
            string? result = await _scanner.ScanImageAsync();

            if (!string.IsNullOrEmpty(result))
            {
                AddLog($"Scanned from Image: {result}");
                await CheckTicketAsync(result, isScan: true);
            }
            else
            {
                AddLog("Image scan canceled or empty.");
            }
        }
        catch (Exception ex)
        {
            AddLog($"Image Scan Error: {ex.Message}");
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

        // The input is just the code
        AddLog($"Checking Manual Code: {ManualCode}");

        await CheckTicketAsync(ManualCode, isScan: false);
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

        // QR Code JSON parsing logic based on user input:
        // {"Secret": "...", "OrderNo": "...", ...}
        // We need "Secret" as the code.
        if (isScan && codeInfo.Trim().StartsWith("{"))
        {
            try
            {
                // Try case-insensitive parsing for robust JSON handling
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                using (JsonDocument doc = JsonDocument.Parse(codeInfo))
                {
                    // Check for "Secret" (Standard from user instruction)
                    if (doc.RootElement.TryGetProperty("Secret", out JsonElement secretElement))
                    {
                        rawCode = secretElement.GetString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Warning: Failed to parse QR JSON: {ex.Message}. Using raw string.");
            }
        }

        // Validate Ticket
        var result = await _ticketService.ValidateTicketAsync(ProductId, rawCode);

        if (result == null)
        {
            ResultText = "Network Error";
            IsResultOk = false;
            AddLog("Ticket Result: Network or Parse Error");
            return;
        }

        if (result.Status == "ok" && result.Code == 200)
        {
            // Success
            string ticketCode = rawCode;
            // If we have secret in response, maybe show it? But the requirement says OK: TicketCode
            // Or maybe "OK: <Secret>"?
            // The example response shows "Secret": "MY-SECRET-CODE" in data.

            ResultText = $"OK: {ticketCode}";
            IsResultOk = true;
            AddLog($"Ticket Valid: OK. Type: {result.Data?.Category ?? "-"}");
        }
        else if (result.Code == 600)
        {
            // Already Used
            ResultText = "Used / 已使用";
            IsResultOk = false; // or maybe warning color? Requirement says Red for Fail, Green for OK. Used is technically a failure to validate as new.
            AddLog($"Ticket Result: Already Used ({result.Msg})");
        }
        else if (result.Code == 500)
        {
            // Expired
            ResultText = "Expired / 已过期";
            IsResultOk = false;
            AddLog($"Ticket Result: Expired ({result.Msg})");
        }
        else if (result.Code == 400 || result.Code == 404)
        {
            // Not found or error
            ResultText = "Failure / 无效票";
            IsResultOk = false;
            AddLog($"Ticket Result: Invalid ({result.Code} - {result.Msg})");
        }
        else
        {
            // Other error
            ResultText = $"Error: {result.Code}";
            IsResultOk = false;
            AddLog($"Ticket Result: Error ({result.Code} - {result.Msg})");
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
        if (string.IsNullOrEmpty(baseUrl)) baseUrl = "https://www.80fafa.com";

        var hash = _configService.CurrentConfig.ProductHashcode;

        // Ensure no double slashes
        baseUrl = baseUrl.TrimEnd('/');
        var url = $"{baseUrl}/Goods/{hash}";

        if (string.IsNullOrEmpty(hash))
        {
            AddLog("Warning: Product Hashcode is empty. Link might be invalid.");
        }

        OpenUrl(url);
    }

    private void OpenUrl(string url)
    {
        // Prioritize platform-specific launcher (e.g., Android Intent)
        if (TicketEasy.App.UrlLauncher != null)
        {
            TicketEasy.App.UrlLauncher.Invoke(url);
            return;
        }

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
