using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>System.Text.Json ile minify / format. UTF-8 safe.</summary>
public sealed class JsonMinifierService : IJsonMinifierService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public int MaxInputLength => MaxInputLengthLimit;

    public Task<ToolResult<JsonMinReport>> MinifyAsync(string? input, CancellationToken ct)
        => ProcessAsync(input, 0, ct);

    public Task<ToolResult<JsonMinReport>> FormatAsync(string? input, int indentSize, CancellationToken ct)
        => ProcessAsync(input, Math.Max(1, indentSize), ct);

    private static Task<ToolResult<JsonMinReport>> ProcessAsync(string? input, int indent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<JsonMinReport>(ToolErrorKind.Validation, JsonMinifierErrorCodes.EmptyInput, "Input is empty.");
        if (input.Length > MaxInputLengthLimit)
            return Fail<JsonMinReport>(ToolErrorKind.TooLarge, JsonMinifierErrorCodes.InputTooLarge, "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            using var doc = JsonDocument.Parse(input);
            using var stream = new MemoryStream();
            var writerOptions = new JsonWriterOptions
            {
                Indented = indent > 0,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            using (var writer = new Utf8JsonWriter(stream, writerOptions))
            {
                doc.WriteTo(writer);
            }
            var output = Encoding.UTF8.GetString(stream.ToArray());
            sw.Stop();
            return Task.FromResult(ToolResult<JsonMinReport>.Ok(new JsonMinReport(
                output, input.Length, output.Length, CountNodes(doc.RootElement), sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<JsonMinReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (JsonException ex) { return Fail<JsonMinReport>(ToolErrorKind.InvalidFormat, JsonMinifierErrorCodes.InvalidJson, ex.Message); }
        catch (Exception ex) { return Fail<JsonMinReport>(ToolErrorKind.Unexpected, JsonMinifierErrorCodes.Unexpected, ex.Message); }
    }

    private static int CountNodes(JsonElement el)
    {
        int count = 1;
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in el.EnumerateObject()) count += CountNodes(p.Value);
                break;
            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray()) count += CountNodes(item);
                break;
        }
        return count;
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}