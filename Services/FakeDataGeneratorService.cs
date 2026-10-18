using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>Kriptografik rastgele ile fake veri üretir.</summary>
public sealed class FakeDataGeneratorService : IFakeDataGeneratorService
{
    public const int MinCountLimit = 1;
    public const int MaxCountLimit = 100;
    public int MinCount => MinCountLimit;
    public int MaxCount => MaxCountLimit;

    private static readonly string[] FirstNames = { "Ahmet", "Mehmet", "Ali", "Ayşe", "Fatma", "Zeynep", "Mustafa", "Can", "Elif", "Emre", "Deniz", "Selin" };
    private static readonly string[] LastNames = { "Yılmaz", "Demir", "Kaya", "Çelik", "Şahin", "Öztürk", "Arslan", "Polat", "Aydın", "Koç" };
    private static readonly string[] Companies = { "TechCorp", "DataSoft", "CyberSec", "DevStudio", "CloudNet", "AI Labs", "SoftWare", "NetSolve" };
    private static readonly string[] Cities = { "İstanbul", "Ankara", "İzmir", "Bursa", "Antalya", "Kocaeli", "Gaziantep" };
    private static readonly string[] Streets = { "Atatürk Cad.", "İstiklal Cad.", "Cumhuriyet Cad.", "Kurtuluş Sok.", "Barış Sok.", "Özgürlük Cad." };

    public Task<ToolResult<FakeDataReport>> GenerateAsync(FakeDataOptions o, CancellationToken ct)
    {
        if (o is null || o.Count < MinCountLimit || o.Count > MaxCountLimit)
            return Fail<FakeDataReport>(ToolErrorKind.Validation, FakeDataGeneratorErrorCodes.InvalidCount,
                $"Count must be {MinCountLimit}..{MaxCountLimit}.");

        var fields = new List<string>();
        if (o.IncludeName) fields.Add("name");
        if (o.IncludeEmail) fields.Add("email");
        if (o.IncludePhone) fields.Add("phone");
        if (o.IncludeAddress) fields.Add("address");
        if (o.IncludeCompany) fields.Add("company");
        if (fields.Count == 0)
            return Fail<FakeDataReport>(ToolErrorKind.Validation, FakeDataGeneratorErrorCodes.NoFieldsSelected, "No fields selected.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            using var stream = new MemoryStream();
            var writerOpts = new JsonWriterOptions { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            using (var writer = new Utf8JsonWriter(stream, writerOpts))
            {
                if (o.Count == 1)
                {
                    WriteRecord(writer, o, ct);
                }
                else
                {
                    writer.WriteStartArray();
                    for (int i = 0; i < o.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        WriteRecord(writer, o, ct);
                    }
                    writer.WriteEndArray();
                }
            }
            sw.Stop();
            var json = Encoding.UTF8.GetString(stream.ToArray());
            return Task.FromResult(ToolResult<FakeDataReport>.Ok(new FakeDataReport(json, o.Count, fields, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<FakeDataReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<FakeDataReport>(ToolErrorKind.Unexpected, FakeDataGeneratorErrorCodes.Unexpected, ex.Message); }
    }

    private static void WriteRecord(Utf8JsonWriter w, FakeDataOptions o, CancellationToken ct)
    {
        w.WriteStartObject();
        if (o.IncludeName) w.WriteString("name", $"{Pick(FirstNames)} {Pick(LastNames)}");
        if (o.IncludeEmail) w.WriteString("email", $"{Pick(FirstNames).ToLowerInvariant()}.{Pick(LastNames).ToLowerInvariant()}@example.com");
        if (o.IncludePhone) w.WriteString("phone", $"+90 5{RandomNumberGenerator.GetInt32(30, 60)} {RandomNumberGenerator.GetInt32(100, 999)} {RandomNumberGenerator.GetInt32(10, 99)} {RandomNumberGenerator.GetInt32(10, 99)}");
        if (o.IncludeAddress) w.WriteString("address", $"{Pick(Streets)} No: {RandomNumberGenerator.GetInt32(1, 100)}, {Pick(Cities)}");
        if (o.IncludeCompany) w.WriteString("company", Pick(Companies));
        w.WriteEndObject();
    }

    private static string Pick(string[] arr) => arr[RandomNumberGenerator.GetInt32(arr.Length)];

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}