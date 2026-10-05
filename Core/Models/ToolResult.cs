namespace tool_cantam.Core.Models;

public sealed record ToolResult
{
    public bool Success { get; init; }
    public string? Output { get; init; }
    public string? ErrorCode { get; init; }
    public string? UserFriendlyError { get; init; }

    public static ToolResult Ok(string output) => new() { Success = true, Output = output };
    public static ToolResult Fail(string code, string msg) => new() { Success = false, ErrorCode = code, UserFriendlyError = msg };
}