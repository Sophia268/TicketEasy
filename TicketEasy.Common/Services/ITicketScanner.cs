using System.Threading.Tasks;

namespace TicketEasy.Services;

public interface ITicketScanner
{
    Task<string?> ScanAsync();
}
