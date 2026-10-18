using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>System.Xml ile XML formatla / minify / doğrula.</summary>
public sealed class XmlFormatterService : IXmlFormatterService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public int MaxInputLength => MaxInputLengthLimit;

    public Task<ToolResult<XmlReport>> FormatAsync(string? input, XmlOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<XmlReport>(ToolErrorKind.Validation, XmlFormatterErrorCodes.EmptyInput, "Input is empty.");
        if (input.Length > MaxInputLengthLimit)
            return Fail<XmlReport>(ToolErrorKind.TooLarge, XmlFormatterErrorCodes.InputTooLarge, "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var doc = new XmlDocument { PreserveWhitespace = false };
            doc.LoadXml(input);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = new string(' ', Math.Max(1, options.IndentSize)),
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = options.OmitDeclaration || doc.FirstChild is not XmlDeclaration,
                Encoding = new UTF8Encoding(false),
            };

            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb, settings))
                doc.Save(writer);

            sw.Stop();
            return Task.FromResult(ToolResult<XmlReport>.Ok(new XmlReport(sb.ToString(), input.Length, sb.Length, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<XmlReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (XmlException ex) { return Fail<XmlReport>(ToolErrorKind.InvalidFormat, XmlFormatterErrorCodes.InvalidXml, ex.Message); }
        catch (Exception ex) { return Fail<XmlReport>(ToolErrorKind.Unexpected, XmlFormatterErrorCodes.Unexpected, ex.Message); }
    }

    public Task<ToolResult<XmlReport>> MinifyAsync(string? input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<XmlReport>(ToolErrorKind.Validation, XmlFormatterErrorCodes.EmptyInput, "Input is empty.");
        if (input.Length > MaxInputLengthLimit)
            return Fail<XmlReport>(ToolErrorKind.TooLarge, XmlFormatterErrorCodes.InputTooLarge, "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var doc = new XmlDocument { PreserveWhitespace = false };
            doc.LoadXml(input);

            var settings = new XmlWriterSettings { Indent = false, NewLineHandling = NewLineHandling.None, OmitXmlDeclaration = true };
            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb, settings))
                doc.Save(writer);

            sw.Stop();
            return Task.FromResult(ToolResult<XmlReport>.Ok(new XmlReport(sb.ToString(), input.Length, sb.Length, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<XmlReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (XmlException ex) { return Fail<XmlReport>(ToolErrorKind.InvalidFormat, XmlFormatterErrorCodes.InvalidXml, ex.Message); }
        catch (Exception ex) { return Fail<XmlReport>(ToolErrorKind.Unexpected, XmlFormatterErrorCodes.Unexpected, ex.Message); }
    }

    public Task<ToolResult<XmlValidation>> ValidateAsync(string? input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<XmlValidation>(ToolErrorKind.Validation, XmlFormatterErrorCodes.EmptyInput, "Input is empty.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var doc = new XmlDocument();
            doc.LoadXml(input);

            int elements = 0, attrs = 0;
            CountNodes(doc.DocumentElement, ref elements, ref attrs);

            return Task.FromResult(ToolResult<XmlValidation>.Ok(new XmlValidation(true, null, elements, attrs)));
        }
        catch (OperationCanceledException) { return Fail<XmlValidation>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (XmlException ex) { return Task.FromResult(ToolResult<XmlValidation>.Ok(new XmlValidation(false, ex.Message, 0, 0))); }
        catch (Exception ex) { return Fail<XmlValidation>(ToolErrorKind.Unexpected, XmlFormatterErrorCodes.Unexpected, ex.Message); }
    }

    private static void CountNodes(XmlNode? node, ref int elements, ref int attrs)
    {
        if (node is null) return;
        if (node.NodeType == XmlNodeType.Element) elements++;
        if (node.Attributes is not null) attrs += node.Attributes.Count;
        foreach (XmlNode child in node.ChildNodes) CountNodes(child, ref elements, ref attrs);
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}