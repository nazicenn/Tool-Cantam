using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces;

public interface IPwnedService
{
    Task<int> CheckPasswordAsync(string password);
}