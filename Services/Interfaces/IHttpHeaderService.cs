using System.Collections.Generic;
using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces;

public interface IHttpHeaderService
{
    Task<Dictionary<string, string>> GetHeadersAsync(string url);
    bool IsValidUrl(string url);
}