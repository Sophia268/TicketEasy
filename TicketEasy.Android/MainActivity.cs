using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using TicketEasy.Android.Services;
using ZXing.Mobile;

namespace TicketEasy.Android;

[Activity(
    Label = "TicketEasy",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    Name = "com.TicketEasy.app.MainActivity",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        try
        {
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            App.Scanner = new AndroidTicketScanner();
            App.UrlLauncher = (url) =>
            {
                try
                {
                    var uri = Android.Net.Uri.Parse(url);
                    var intent = new Android.Content.Intent(Android.Content.Intent.ActionView, uri);
                    StartActivity(intent);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error launching URL: {ex}");
                }
            };

            base.OnCreate(savedInstanceState);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FATAL ERROR in MainActivity.OnCreate: {ex}");
            // Log to Android logcat as well
            global::Android.Util.Log.Error("TicketEasy", $"FATAL ERROR in MainActivity.OnCreate: {ex}");
            throw; // Re-throw to let the app crash visibly if we can't recover
        }
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
