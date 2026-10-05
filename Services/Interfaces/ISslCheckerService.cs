using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ISslCheckerService
{
    Task<SslCertificateInfo> CheckAsync(string domain);
    bool IsValidDomain(string domain);
}