namespace tool_cantam.Core.Contracts;

public interface IToolLogger
{
    void LogError(Exception ex, string toolName);
    void LogExecution(string toolName, TimeSpan duration, bool success);
}