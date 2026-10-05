using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;

namespace tool_cantam.Services;

public sealed class ToolLogger : IToolLogger
{
    private readonly ILogger<ToolLogger> _logger;

    public ToolLogger(ILogger<ToolLogger> logger)
    {
        _logger = logger;
    }

    public void LogError(Exception ex, string toolName)
    {
        _logger.LogError(ex, "Tool '{ToolName}' failed: {Message}", toolName, ex.Message);
    }

    public void LogExecution(string toolName, TimeSpan duration, bool success)
    {
        if (success)
            _logger.LogInformation("Tool '{ToolName}' OK ({Duration}ms)", toolName, (int)duration.TotalMilliseconds);
        else
            _logger.LogWarning("Tool '{ToolName}' FAIL ({Duration}ms)", toolName, (int)duration.TotalMilliseconds);
    }
}