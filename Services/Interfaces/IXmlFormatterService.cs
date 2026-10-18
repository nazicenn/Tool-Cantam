using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IXmlFormatterService
{
    int MaxInputLength { get; }
    Task<ToolResult<XmlReport>> FormatAsync(string? input, XmlOptions options, CancellationToken ct);
    Task<ToolResult<XmlReport>> MinifyAsync(string? input, CancellationToken ct);
    Task<ToolResult<XmlValidation>> ValidateAsync(string? input, CancellationToken ct);
}

public sealed record XmlOptions(int IndentSize, bool OmitDeclaration);
public sealed record XmlReport(string Output, int OriginalLength, int OutputLength, TimeSpan Elapsed);
public sealed record XmlValidation(bool IsValid, string? Error, int ElementCount, int AttributeCount);