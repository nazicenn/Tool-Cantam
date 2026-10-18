using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// SQL formatter — token bazlı, keyword uppercase, newline + indent.
/// </summary>
public sealed class SqlFormatterService : ISqlFormatterService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;

    private static readonly HashSet<string> NewlineKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "FROM", "WHERE", "HAVING", "UNION", "UNION ALL", "EXCEPT", "INTERSECT",
        "ORDER BY", "GROUP BY", "LIMIT", "OFFSET", "INSERT INTO", "UPDATE", "DELETE FROM",
        "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN", "CROSS JOIN",
        "LEFT OUTER JOIN", "RIGHT OUTER JOIN", "FULL OUTER JOIN", "ON", "SET", "VALUES", "INTO",
    };

    private static readonly HashSet<string> UpperKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "FROM", "WHERE", "AND", "OR", "NOT", "NULL", "IS", "IN", "LIKE", "BETWEEN",
        "ORDER", "BY", "GROUP", "HAVING", "LIMIT", "OFFSET", "UNION", "ALL", "EXCEPT", "INTERSECT",
        "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE", "JOIN", "INNER", "LEFT", "RIGHT",
        "FULL", "OUTER", "CROSS", "ON", "AS", "DISTINCT", "COUNT", "SUM", "AVG", "MIN", "MAX",
        "ASC", "DESC", "CASE", "WHEN", "THEN", "ELSE", "END", "EXISTS", "CREATE", "TABLE",
        "DROP", "ALTER", "INDEX", "PRIMARY", "KEY", "FOREIGN", "REFERENCES", "DEFAULT", "CHECK",
    };

    public int MaxInputLength => MaxInputLengthLimit;

    public Task<ToolResult<SqlReport>> FormatAsync(string? input, SqlOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<SqlReport>(ToolErrorKind.Validation, SqlFormatterErrorCodes.EmptyInput, "Input is empty.");
        if (input.Length > MaxInputLengthLimit)
            return Fail<SqlReport>(ToolErrorKind.TooLarge, SqlFormatterErrorCodes.InputTooLarge, "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var tokens = Tokenize(input, ct);
            var formatted = Emit(tokens, options, ct);
            sw.Stop();
            return Task.FromResult(ToolResult<SqlReport>.Ok(
                new SqlReport(formatted, input.Length, formatted.Length, sw.Elapsed)));
        }
        catch (OperationCanceledException)
        {
            return Fail<SqlReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (InvalidOperationException ex)
        {
            return Fail<SqlReport>(ToolErrorKind.InvalidFormat, SqlFormatterErrorCodes.UnbalancedParen, ex.Message);
        }
        catch (Exception ex)
        {
            return Fail<SqlReport>(ToolErrorKind.Unexpected, SqlFormatterErrorCodes.Unexpected, ex.Message);
        }
    }

    public Task<ToolResult<SqlReport>> MinifyAsync(string? input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<SqlReport>(ToolErrorKind.Validation, SqlFormatterErrorCodes.EmptyInput, "Input is empty.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var sb = new StringBuilder();
            bool lastWasSpace = false;
            bool inString = false;
            char quote = '\0';

            foreach (var c in input)
            {
                ct.ThrowIfCancellationRequested();
                if (inString)
                {
                    sb.Append(c);
                    if (c == quote) inString = false;
                    continue;
                }
                if (c == '\'' || c == '"')
                {
                    sb.Append(c);
                    inString = true; quote = c; lastWasSpace = false;
                    continue;
                }
                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace) { sb.Append(' '); lastWasSpace = true; }
                    continue;
                }
                sb.Append(c);
                lastWasSpace = false;
            }

            var result = sb.ToString().Trim();
            sw.Stop();
            return Task.FromResult(ToolResult<SqlReport>.Ok(
                new SqlReport(result, input.Length, result.Length, sw.Elapsed)));
        }
        catch (OperationCanceledException)
        {
            return Fail<SqlReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<SqlReport>(ToolErrorKind.Unexpected, SqlFormatterErrorCodes.Unexpected, ex.Message);
        }
    }

    public Task<ToolResult<SqlValidation>> ValidateAsync(string? input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<SqlValidation>(ToolErrorKind.Validation, SqlFormatterErrorCodes.EmptyInput, "Input is empty.");

        try
        {
            ct.ThrowIfCancellationRequested();
            int paren = 0, stmt = 0;
            bool inString = false;
            char quote = '\0';

            foreach (var c in input)
            {
                ct.ThrowIfCancellationRequested();
                if (inString)
                {
                    if (c == quote) inString = false;
                    continue;
                }
                if (c == '\'' || c == '"') { inString = true; quote = c; continue; }
                if (c == '(') paren++;
                else if (c == ')') { paren--; if (paren < 0) return Task.FromResult(ToolResult<SqlValidation>.Ok(new SqlValidation(false, "Unbalanced parentheses", stmt))); }
                else if (c == ';') stmt++;
            }

            if (inString) return Task.FromResult(ToolResult<SqlValidation>.Ok(new SqlValidation(false, "Unclosed string literal", stmt)));
            if (paren != 0) return Task.FromResult(ToolResult<SqlValidation>.Ok(new SqlValidation(false, "Unbalanced parentheses", stmt)));

            return Task.FromResult(ToolResult<SqlValidation>.Ok(new SqlValidation(true, null, stmt + 1)));
        }
        catch (OperationCanceledException)
        {
            return Fail<SqlValidation>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<SqlValidation>(ToolErrorKind.Unexpected, SqlFormatterErrorCodes.Unexpected, ex.Message);
        }
    }

    // ---- Tokenizer ----

    private enum TokenKind { Word, String, Number, Punct, Whitespace, Comment }

    private sealed record Token(TokenKind Kind, string Text);

    private static List<Token> Tokenize(string input, CancellationToken ct)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < input.Length)
        {
            ct.ThrowIfCancellationRequested();
            char c = input[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            // Comments: -- veya /* */
            if (c == '-' && i + 1 < input.Length && input[i + 1] == '-')
            {
                int end = input.IndexOf('\n', i); if (end < 0) end = input.Length;
                tokens.Add(new Token(TokenKind.Comment, input[i..end]));
                i = end;
                continue;
            }
            if (c == '/' && i + 1 < input.Length && input[i + 1] == '*')
            {
                int end = input.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? input.Length : end + 2;
                tokens.Add(new Token(TokenKind.Comment, input[i..end]));
                i = end;
                continue;
            }

            // String
            if (c == '\'' || c == '"')
            {
                int start = i; i++;
                while (i < input.Length && input[i] != c) i++;
                if (i < input.Length) i++;
                tokens.Add(new Token(TokenKind.String, input[start..Math.Min(i, input.Length)]));
                continue;
            }

            // Number
            if (char.IsDigit(c) || (c == '.' && i + 1 < input.Length && char.IsDigit(input[i + 1])))
            {
                int start = i;
                while (i < input.Length && (char.IsDigit(input[i]) || input[i] == '.')) i++;
                tokens.Add(new Token(TokenKind.Number, input[start..i]));
                continue;
            }

            // Word (identifier/keyword)
            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_')) i++;
                tokens.Add(new Token(TokenKind.Word, input[start..i]));
                continue;
            }

            // Punct
            tokens.Add(new Token(TokenKind.Punct, c.ToString()));
            i++;
        }
        return tokens;
    }

    private static string Emit(List<Token> tokens, SqlOptions o, CancellationToken ct)
    {
        var sb = new StringBuilder();
        int indent = 0;
        string indentStr = new(' ', o.IndentSize);
        bool atLineStart = true;

        void Append(string text)
        {
            if (atLineStart) { sb.Append(new string(' ', indent * o.IndentSize)); atLineStart = false; }
            sb.Append(text);
        }
        void NewLine()
        {
            sb.AppendLine();
            atLineStart = true;
        }

        for (int k = 0; k < tokens.Count; k++)
        {
            ct.ThrowIfCancellationRequested();
            var t = tokens[k];

            if (t.Kind == TokenKind.Comment)
            {
                NewLine();
                Append(t.Text);
                NewLine();
                continue;
            }

            // Çoklu kelime keyword kontrolü (ORDER BY, GROUP BY, INNER JOIN vs.)
            string maybeMulti = t.Text;
            int lookahead = k + 1;
            while (lookahead < tokens.Count && tokens[lookahead].Kind == TokenKind.Word && maybeMulti.Length < 20)
            {
                var candidate = maybeMulti + " " + tokens[lookahead].Text;
                if (NewlineKeywords.Contains(candidate) || UpperKeywords.Contains(candidate))
                {
                    maybeMulti = candidate;
                    lookahead++;
                }
                else break;
            }

            bool isMulti = lookahead > k + 1;
            string wordText = isMulti ? maybeMulti : t.Text;
            bool isNewlineKw = NewlineKeywords.Contains(wordText);
            bool isUpperKw = o.UppercaseKeywords && UpperKeywords.Contains(wordText);
            bool isAndOr = wordText.Equals("AND", StringComparison.OrdinalIgnoreCase)
                        || wordText.Equals("OR", StringComparison.OrdinalIgnoreCase);

            if (isNewlineKw)
            {
                if (!atLineStart) NewLine();
                Append(isUpperKw ? wordText.ToUpperInvariant() : wordText);
                NewLine();
            }
            else if (isAndOr && o.NewlineBeforeAndOr)
            {
                NewLine();
                Append(isUpperKw ? wordText.ToUpperInvariant() : wordText);
                sb.Append(' ');
            }
            else if (t.Kind == TokenKind.Punct && t.Text == "(")
            {
                Append(t.Text);
                indent++;
                NewLine();
            }
            else if (t.Kind == TokenKind.Punct && t.Text == ")")
            {
                indent = Math.Max(0, indent - 1);
                if (!atLineStart) NewLine();
                Append(t.Text);
            }
            else if (t.Kind == TokenKind.Punct && t.Text == ",")
            {
                Append(t.Text);
                if (o.CommaNewline) NewLine();
                else sb.Append(' ');
            }
            else
            {
                Append(isUpperKw ? wordText.ToUpperInvariant() : wordText);
                if (isMulti) k = lookahead - 1;
                sb.Append(' ');
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string msg)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, msg));
}