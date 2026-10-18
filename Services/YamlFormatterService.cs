using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using YamlDotNet.Serialization;

namespace tool_cantam.Services;

/// <summary>YamlDotNet ile formatla / minify.</summary>
public sealed class YamlFormatterService : IYamlFormatterService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public int MaxInputLength => MaxInputLengthLimit;

    public Task<ToolResult<YamlReport>> FormatAsync(string? input, int indentSize, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<YamlReport>(ToolErrorKind.Validation, YamlFormatterErrorCodes.EmptyInput, "Input is empty.");
        if (input.Length > MaxInputLengthLimit)
            return Fail<YamlReport>(ToolErrorKind.TooLarge, YamlFormatterErrorCodes.InputTooLarge, "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var deserializer = new DeserializerBuilder().Build();
            var obj = deserializer.Deserialize<object>(input);
            var serializer = new SerializerBuilder()
                .WithIndentedSequences()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build();
            var output = serializer.Serialize(obj);
            sw.Stop();
            return Task.FromResult(ToolResult<YamlReport>.Ok(new YamlReport(output, input.Length, output.Length, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<YamlReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (YamlDotNet.Core.YamlException ex) { return Fail<YamlReport>(ToolErrorKind.InvalidFormat, YamlFormatterErrorCodes.InvalidYaml, ex.Message); }
        catch (Exception ex) { return Fail<YamlReport>(ToolErrorKind.Unexpected, YamlFormatterErrorCodes.Unexpected, ex.Message); }
    }

    public Task<ToolResult<YamlReport>> MinifyAsync(string? input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<YamlReport>(ToolErrorKind.Validation, YamlFormatterErrorCodes.EmptyInput, "Input is empty.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var deserializer = new DeserializerBuilder().Build();
            var obj = deserializer.Deserialize<object>(input);
            var serializer = new SerializerBuilder().Build();
            var output = serializer.Serialize(obj).TrimEnd();
            sw.Stop();
            return Task.FromResult(ToolResult<YamlReport>.Ok(new YamlReport(output, input.Length, output.Length, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<YamlReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (YamlDotNet.Core.YamlException ex) { return Fail<YamlReport>(ToolErrorKind.InvalidFormat, YamlFormatterErrorCodes.InvalidYaml, ex.Message); }
        catch (Exception ex) { return Fail<YamlReport>(ToolErrorKind.Unexpected, YamlFormatterErrorCodes.Unexpected, ex.Message); }
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}