using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using TicketEasy.Services;
using TicketEasy.ViewModels;
using TicketEasy.Views;

namespace TicketEasy;

public partial class App : Application
{
    public static ITicketScanner? Scanner { get; set; }
    public static Action<string>? UrlLauncher { get; set; }

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Default;
        DataTemplates.Add(new ViewLocator());
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainWindowViewModel(Scanner)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
