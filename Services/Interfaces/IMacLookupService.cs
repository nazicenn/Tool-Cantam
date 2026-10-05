using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces;

public interface IMacLookupService
{
    Task<string> GetVendorAsync(string mac);
    bool IsValidMac(string mac);
}