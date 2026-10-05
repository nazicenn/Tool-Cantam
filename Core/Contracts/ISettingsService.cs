using tool_cantam.Core.Models;

namespace tool_cantam.Core.Contracts;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}