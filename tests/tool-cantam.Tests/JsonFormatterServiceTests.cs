using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// JSON biçimlendirme ve doğrulamanın tek uygulaması.
/// <para>
/// Kurallar: max 10 MB girdi, max 256 derinlik, geçersiz JSON satır/kolon ile raporlanır.
/// Boyut kontrolü boşluk kontrolünden ÖNCE yapılır.
/// </para>
/// </summary>
public sealed class JsonFormatterService : IJsonFormatterService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;
    public const int MaxDepthLimit = 256;

    public int MaxTextLength => MaxTextLengthLimit;
    public int MaxDepth => MaxDepthLimit;

    public ToolResult<JsonFormatReport> Format(string? json, int indentSize)
    {
        if (indentSize < 1 || indentSize > 8)
            return ToolResult<JsonFormatReport>.Fail(
                ToolErrorKind.Validation,
                JsonFormatterErrorCodes.InvalidIndent,
                $"Indent size must be 1..8 (got {indentSize}).");

        return Process(json, indented: true, indentSize);
    }

    public ToolResult<JsonFormatReport> Minify(string? json) =>
        Process(json, indented: false, indentSize: 0);

    public ToolResult<JsonFormatReport> Validate(string? json)
    {
        var result = Process(json, indented: false, indentSize: 0);
        if (!result.IsSuccess) return result;

        return ToolResult<JsonFormatReport>.Ok(result.Value! with { Output = string.Empty });
    }

    private ToolResult<JsonFormatReport> Process(string? json, bool indented, int indentSize)
    {
        // ★ 1. ÖNCE boyut kontrolü (boşluk kontrolünden önce)
        if (json is not null && json.Length > MaxTextLengthLimit)
            return ToolResult<JsonFormatReport>.Fail(
                ToolErrorKind.TooLarge,
                JsonFormatterErrorCodes.InputTooLarge,
                $"Input length {json.Length} exceeds {MaxTextLengthLimit}.");

        // ★ 2. SONRA boşluk kontrolü
        if (string.IsNullOrWhiteSpace(json))
            return ToolResult<JsonFormatReport>.Fail(
                ToolErrorKind.Validation,
                JsonFormatterErrorCodes.EmptyInput,
                "JSON input is empty.");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var parseOptions = new JsonDocumentOptions
            {
                MaxDepth = MaxDepthLimit,
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            };

            using var document = JsonDocument.Parse(json, parseOptions);
            var root = document.RootElement;

            int nodeCount = CountNodes(root);
            int depth = MeasureDepth(root, 0);

            string output;
            if (indented)
            {
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
                {
                    Indented = true,
                    IndentSize = indentSize,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }))
                {
                    root.WriteTo(writer);
                    writer.Flush();
                }
                output = Encoding.UTF8.GetString(stream.ToArray());
            }
            else
            {
                output = JsonSerializer.Serialize(root,
                    new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = false
                    });
            }

            stopwatch.Stop();

            return ToolResult<JsonFormatReport>.Ok(new JsonFormatReport(
                output,
                nodeCount,
                depth,
                Encoding.UTF8.GetByteCount(json),
                Encoding.UTF8.GetByteCount(output),
                stopwatch.Elapsed));
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();

            string detail = $"Line {ex.LineNumber ?? 0}, position {ex.BytePositionInLine ?? 0}: {ex.Message}";
            return ToolResult<JsonFormatReport>.Fail(
                ToolErrorKind.InvalidFormat,
                JsonFormatterErrorCodes.InvalidJson,
                detail);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResult<JsonFormatReport>.Fail(
                ToolErrorKind.Unexpected,
                JsonFormatterErrorCodes.Unexpected,
                ex.Message);
        }
    }

    private static int CountNodes(JsonElement element)
    {
        int count = 1;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
                count += CountNodes(prop.Value);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                count += CountNodes(item);
        }

        return count;
    }

    private static int MeasureDepth(JsonElement element, int currentDepth)
    {
        int max = currentDepth + 1;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
                max = Math.Max(max, MeasureDepth(prop.Value, currentDepth + 1));
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                max = Math.Max(max, MeasureDepth(item, currentDepth + 1));
        }

        return max;
    }
}