using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IIpInfoService
{
    Task<IpInfoModel?> GetIpInfoAsync(string ip);
    bool IsValidIp(string ip);
}