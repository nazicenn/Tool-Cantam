using System.Collections.Generic;
using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces;

public interface ISubdomainService
{
    Task<List<string>> ScanAsync(string domain);
    bool IsValidDomain(string domain);
}