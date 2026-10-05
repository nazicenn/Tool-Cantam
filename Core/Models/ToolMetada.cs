using System;
using tool_cantam.Core.Enums;

namespace tool_cantam.Core.Models;

public class ToolMetadata
{
    public string ToolName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ToolCategory Category { get; set; }
    public ExecutionType ExecutionType { get; set; }
    public string IconKey { get; set; } = string.Empty;
    public int Priority { get; set; }
    public Type PageType { get; set; } = null!;
}