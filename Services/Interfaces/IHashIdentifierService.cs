using System.Collections.Generic;

namespace tool_cantam.Services.Interfaces;

public interface IHashIdentifierService
{
    List<string> Identify(string hash);
}