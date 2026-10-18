using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>CSV → JSON. RFC 4180 uyumlu quote escaping, tip çıkarımı.</summary>
public sealed class CsvToJsonService : ICsvToJsonService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public int MaxInputLength => MaxInputLengthLimit;

    public Task<ToolResult<CsvReport>> ConvertAsync(string? input, CsvOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<CsvReport>(ToolErrorKind.Validation, CsvToJsonErrorCodes.EmptyInput, "Input is empty.");
        if (input.Length > MaxInputLengthLimit)
            return Fail<CsvReport>(ToolErrorKind.TooLarge, CsvToJsonErrorCodes.InputTooLarge, "Input too large.");
        if (options is null || options.Delimiter == '\0')
            return Fail<CsvReport>(ToolErrorKind.Validation, CsvToJsonErrorCodes.InvalidDelimiter, "Invalid delimiter.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var rows = ParseCsv(input, options.Delimiter, ct);
            if (rows.Count == 0)
                return Fail<CsvReport>(ToolErrorKind.Validation, CsvToJsonErrorCodes.EmptyInput, "No rows parsed.");

            var headers = options.FirstRowAsHeader
                ? rows[0]
                : Enumerable.Range(1, rows[0].Count).Select(i => $"Column{i}").ToList();

            int start = options.FirstRowAsHeader ? 1 : 0;
            int colCount = headers.Count;

            // Tüm satırlar aynı sayıda sütun mu?
            for (int i = start; i < rows.Count; i++)
            {
                if (rows[i].Count != colCount)
                    return Fail<CsvReport>(ToolErrorKind.InvalidFormat, CsvToJsonErrorCodes.InconsistentColumns,
                        $"Row {i + 1} has {rows[i].Count} columns, expected {colCount}.");
            }

            var sb = new StringBuilder();
            sb.AppendLine("[");

            var jsonOpts = new JsonWriterOptions { Indented = options.IndentSize > 0, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

            for (int i = start; i < rows.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                sb.Append("  ");
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(ms, jsonOpts))
                    {
                        writer.WriteStartObject();
                        for (int j = 0; j < colCount; j++)
                        {
                            var key = headers[j];
                            var raw = j < rows[i].Count ? rows[i][j] : string.Empty;
                            if (options.TrimValues) raw = raw.Trim();

                            writer.WritePropertyName(key);
                            if (options.DetectTypes) WriteTyped(writer, raw);
                            else writer.WriteStringValue(raw);
                        }
                        writer.WriteEndObject();
                    }
                    sb.Append(Encoding.UTF8.GetString(ms.ToArray()));
                }
                if (i < rows.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }

            sb.AppendLine("]");
            sw.Stop();
            return Task.FromResult(ToolResult<CsvReport>.Ok(new CsvReport(sb.ToString().TrimEnd(), rows.Count - start, colCount, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<CsvReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<CsvReport>(ToolErrorKind.Unexpected, CsvToJsonErrorCodes.Unexpected, ex.Message); }
    }

    private static void WriteTyped(Utf8JsonWriter w, string raw)
    {
        if (string.IsNullOrEmpty(raw)) { w.WriteNullValue(); return; }
        if (long.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var l)) { w.WriteNumberValue(l); return; }
        if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) { w.WriteNumberValue(d); return; }
        if (bool.TryParse(raw, out var b)) { w.WriteBooleanValue(b); return; }
        w.WriteStringValue(raw);
    }

    /// <summary>RFC 4180 uyumlu CSV parser. "..." içindeki alanlar, "" escape.</summary>
    private static List<List<string>> ParseCsv(string input, char delimiter, CancellationToken ct)
    {
        var rows = new List<List<string>>();
        var current = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;
        int i = 0;
        int n = input.Length;

        while (i < n)
        {
            ct.ThrowIfCancellationRequested();
            char c = input[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < n && input[i + 1] == '"') { field.Append('"'); i += 2; }
                    else { inQuotes = false; i++; }
                }
                else { field.Append(c); i++; }
                continue;
            }

            if (c == '"') { inQuotes = true; i++; continue; }

            if (c == delimiter) { current.Add(field.ToString()); field.Clear(); i++; continue; }

            if (c == '\r') { if (i + 1 < n && input[i + 1] == '\n') i += 2; else i++; current.Add(field.ToString()); field.Clear(); rows.Add(current); current = new List<string>(); continue; }
            if (c == '\n') { i++; current.Add(field.ToString()); field.Clear(); rows.Add(current); current = new List<string>(); continue; }

            field.Append(c);
            i++;
        }

        if (field.Length > 0 || current.Count > 0) { current.Add(field.ToString()); rows.Add(current); }
        // Boş satırları at
        rows.RemoveAll(r => r.Count == 1 && string.IsNullOrEmpty(r[0]));
        return rows;
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}