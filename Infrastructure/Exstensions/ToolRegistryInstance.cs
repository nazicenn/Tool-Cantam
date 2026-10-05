using tool_cantam.Core.Models;

namespace tool_cantam.Infrastructure;

public class ToolRegistryInstance
{
    private readonly List<ToolDefinition> _tools = new();

    public IReadOnlyList<ToolDefinition> Tools => _tools;

    public void Register(ToolDefinition tool)
    {
        _tools.Add(tool);
    }

    public void Clear()
    {
        _tools.Clear();
    }
}