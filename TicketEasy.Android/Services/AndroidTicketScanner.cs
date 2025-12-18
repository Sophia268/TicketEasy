using System.Threading.Tasks;
using TicketEasy.Services;
using ZXing.Mobile;

namespace TicketEasy.Android.Services;

public class AndroidTicketScanner : ITicketScanner
{
    public async Task<string?> ScanAsync()
    {
        var scanner = new MobileBarcodeScanner();
        // Optional: Custom overlay or options
        // var options = new MobileBarcodeScanningOptions();
        // var result = await scanner.Scan(options);

        var result = await scanner.Scan();
        return result?.Text;
    }

    public async Task<string?> ScanImageAsync()
    {
        try
        {
            var photo = await Xamarin.Essentials.MediaPicker.PickPhotoAsync();
            if (photo == null) return null;

            using (var stream = await photo.OpenReadAsync())
            {
                var bitmap = await global::Android.Graphics.BitmapFactory.DecodeStreamAsync(stream);
                if (bitmap == null) return null;

                var reader = new ZXing.Mobile.BarcodeReader();
                var result = reader.Decode(bitmap);
                return result?.Text;
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ScanImageAsync Error: {ex}");
            return null;
        }
    }
}
