using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IWhoisService
{
    Task<WhoisModel> LookupAsync(string domain);
    bool IsValidDomain(string domain);
}