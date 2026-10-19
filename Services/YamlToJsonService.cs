using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using YamlDotNet.Serialization;

namespace tool_cantam.Services;

/// <summary>YamlDotNet + System.Text.Json ile YAML → JSON.</summary>
public sealed class YamlToJsonService : IYamlToJsonService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public int MaxInputLength => MaxInputLengthLimit;

    public Task<ToolResult<YamlToJsonReport>> ConvertAsync(string? yaml, YamlToJsonOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return Fail<YamlToJsonReport>(ToolErrorKind.Validation, YamlToJsonErrorCodes.EmptyInput, "Input is empty.");
        if (yaml.Length > MaxInputLengthLimit)
            return Fail<YamlToJsonReport>(ToolErrorKind.TooLarge, YamlToJsonErrorCodes.InputTooLarge, "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var deserializer = new DeserializerBuilder().Build();
            var obj = deserializer.Deserialize<object>(yaml);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = options.Indented,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(obj, jsonOptions);
            sw.Stop();
            return Task.FromResult(ToolResult<YamlToJsonReport>.Ok(new YamlToJsonReport(json, yaml.Length, json.Length, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<YamlToJsonReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (YamlDotNet.Core.YamlException ex) { return Fail<YamlToJsonReport>(ToolErrorKind.InvalidFormat, YamlToJsonErrorCodes.InvalidYaml, ex.Message); }
        catch (Exception ex) { return Fail<YamlToJsonReport>(ToolErrorKind.Unexpected, YamlToJsonErrorCodes.Unexpected, ex.Message); }
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}