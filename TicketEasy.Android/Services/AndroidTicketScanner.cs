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
}
