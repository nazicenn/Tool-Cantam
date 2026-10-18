using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IFakeDataGeneratorService
{
    int MinCount { get; }
    int MaxCount { get; }
    Task<ToolResult<FakeDataReport>> GenerateAsync(FakeDataOptions options, CancellationToken ct);
}

public sealed record FakeDataOptions(
    int Count, bool IncludeName, bool IncludeEmail, bool IncludePhone, bool IncludeAddress, bool IncludeCompany);

public sealed record FakeDataReport(string Json, int RecordCount, IReadOnlyList<string> Fields, TimeSpan Elapsed);