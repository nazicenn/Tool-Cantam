using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces;

public interface IReverseDnsService
{
    Task<string> LookupAsync(string ip);
    bool IsValidIp(string ip);
}