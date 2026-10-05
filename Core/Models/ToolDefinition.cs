using System;
using System.Threading.Tasks;
using tool_cantam.Core.Enums;

namespace tool_cantam.Core.Models;

public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ToolCategory Category { get; set; }
    public Type ViewModelType { get; set; } = null!;
    public Type ViewType { get; set; } = null!;
    public Func<object, Task<object>> Execute { get; set; } = null!;
    public Type? ExecutorType { get; set; }
}