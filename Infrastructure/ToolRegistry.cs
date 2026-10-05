using System.Collections.Generic;
using tool_cantam.Core.Models;

namespace tool_cantam.Infrastructure;

public static class ToolRegistry
{
    public static List<ToolDefinition> Tools { get; } = new();

    public static void Register(ToolDefinition tool)
    {
        Tools.Add(tool);
    }

    public static void Clear() => Tools.Clear();
}