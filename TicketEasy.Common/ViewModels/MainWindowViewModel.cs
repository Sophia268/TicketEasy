using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicketEasy.Services;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TicketEasy.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly RegistrationService _registrationService;

    [ObservableProperty]
    private string _machineCode = string.Empty;

    [ObservableProperty]
    private string _registrationCode = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isError;

    // Decoded info
    [ObservableProperty]
    private string _decodedMachineId = "-";

    [ObservableProperty]
    private string _periodType = "-";

    [ObservableProperty]
    private string _createTime = "-";

    [ObservableProperty]
    private string _expiredTime = "-";

    [ObservableProperty]
    private bool _showAbout;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    public MainWindowViewModel()
    {
        _registrationService = new RegistrationService();
        MachineCode = MachineIdProvider.GetLocalMachineId();
        AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }

    [RelayCommand]
    private void ToggleAbout()
    {
        ShowAbout = !ShowAbout;
    }

    [RelayCommand]
    private void OpenBlog()
    {
        OpenUrl("https://80fafa.com");
    }

    [RelayCommand]
    private void Register()
    {
        StatusMessage = "";
        IsError = false;

        if (string.IsNullOrWhiteSpace(RegistrationCode))
        {
            StatusMessage = "Please enter registration code";
            IsError = true;
            return;
        }

        if (!_registrationService.TryDecodeRegistrationCode(RegistrationCode, out var info, out var error))
        {
            StatusMessage = $"Invalid code: {error}";
            IsError = true;
            ClearResult();
            return;
        }

        if (info == null) return;

        if (!string.Equals(info.MachineID, MachineCode, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Machine code mismatch";
            IsError = true;
            ClearResult();
            return;
        }

        if (DateTime.UtcNow > info.ExpiredTime.ToUniversalTime())
        {
            StatusMessage = "Code expired";
            IsError = true;
            ClearResult();
            return;
        }

        DecodedMachineId = info.MachineID;
        PeriodType = info.PeriodType.ToString();
        CreateTime = info.CreateTime.ToString("yyyy-MM-dd HH:mm:ss");
        ExpiredTime = info.ExpiredTime.ToString("yyyy-MM-dd HH:mm:ss");

        StatusMessage = "Registration Successful";
        IsError = false;
    }

    [RelayCommand]
    private void Purchase()
    {
        var uri = ConfigProvider.Get().URI;
        if (string.IsNullOrWhiteSpace(uri))
        {
            StatusMessage = "Purchase link not configured";
            IsError = true;
            return;
        }

        try
        {
            OpenUrl(uri);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Cannot open link: {ex.Message}";
            IsError = true;
        }
    }

    private void ClearResult()
    {
        DecodedMachineId = "-";
        PeriodType = "-";
        CreateTime = "-";
        ExpiredTime = "-";
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Cross-platform open url hack
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
