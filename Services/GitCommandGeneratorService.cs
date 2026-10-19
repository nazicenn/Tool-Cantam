using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>Git komut oluşturucu — parametreli komut üretimi.</summary>
public sealed class GitCommandGeneratorService : IGitCommandGeneratorService
{
    public const int MaxInputLengthLimit = 1024;
    public int MaxInputLength => MaxInputLengthLimit;

    public IReadOnlyList<GitCommandCategory> Categories => new[]
    {
        GitCommandCategory.Commit, GitCommandCategory.Branch, GitCommandCategory.Merge,
        GitCommandCategory.Rebase, GitCommandCategory.Reset, GitCommandCategory.Tag,
        GitCommandCategory.Stash, GitCommandCategory.Remote, GitCommandCategory.Log
    };

    public Task<ToolResult<GitCommandReport>> GenerateAsync(GitCommandOptions options, CancellationToken ct)
    {
        if (options is null)
            return Fail<GitCommandReport>(ToolErrorKind.Validation, GitCommandErrorCodes.EmptyArgument, "Options are null.");

        if (options.Argument1?.Length > MaxInputLengthLimit || options.Argument2?.Length > MaxInputLengthLimit)
            return Fail<GitCommandReport>(ToolErrorKind.TooLarge, GitCommandErrorCodes.InputTooLarge, "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var (command, description) = Generate(options);

            sw.Stop();
            return Task.FromResult(ToolResult<GitCommandReport>.Ok(new GitCommandReport(
                command, description, options.Category.ToString(), sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<GitCommandReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<GitCommandReport>(ToolErrorKind.Unexpected, GitCommandErrorCodes.Unexpected, ex.Message); }
    }

    private static (string Command, string Description) Generate(GitCommandOptions o)
    {
        var sb = new StringBuilder("git");
        var desc = "";

        switch (o.Category)
        {
            case GitCommandCategory.Commit:
                sb.Append(" commit");
                if (o.Flag1) sb.Append(" -a");
                if (o.Flag2) sb.Append(" --amend");
                sb.Append($" -m \"{Escape(o.Argument1, "message")}\"");
                desc = "Commit changes with a message.";
                break;

            case GitCommandCategory.Branch:
                sb.Append(" branch");
                if (o.Flag1) sb.Append(" -d");
                if (!string.IsNullOrWhiteSpace(o.Argument1)) sb.Append($" {Escape(o.Argument1, "name")}");
                desc = o.Flag1 ? "Delete a branch." : "Create or list branches.";
                break;

            case GitCommandCategory.Merge:
                sb.Append(" merge");
                if (o.Flag1) sb.Append(" --no-ff");
                if (o.Flag2) sb.Append(" --squash");
                sb.Append($" {Escape(o.Argument1, "branch")}");
                desc = "Merge a branch into the current branch.";
                break;

            case GitCommandCategory.Rebase:
                sb.Append(" rebase");
                if (o.Flag1) sb.Append(" -i");
                sb.Append($" {Escape(o.Argument1, "branch")}");
                desc = "Rebase the current branch onto another.";
                break;

            case GitCommandCategory.Reset:
                sb.Append(" reset");
                if (o.Flag1) sb.Append(" --hard");
                else if (o.Flag2) sb.Append(" --soft");
                sb.Append($" {Escape(o.Argument1, "HEAD~1")}");
                desc = "Reset the current branch to a specific commit.";
                break;

            case GitCommandCategory.Tag:
                sb.Append(" tag");
                if (o.Flag1) sb.Append(" -d");
                if (!string.IsNullOrWhiteSpace(o.Argument1)) sb.Append($" {Escape(o.Argument1, "name")}");
                if (!string.IsNullOrWhiteSpace(o.Argument2) && !o.Flag1) sb.Append($" {Escape(o.Argument2, "commit")}");
                desc = o.Flag1 ? "Delete a tag." : "Create or list tags.";
                break;

            case GitCommandCategory.Stash:
                sb.Append(" stash");
                if (o.Flag1) sb.Append(" pop");
                if (o.Flag2) sb.Append(" list");
                if (!string.IsNullOrWhiteSpace(o.Argument1) && !o.Flag1 && !o.Flag2)
                    sb.Append($" push -m \"{Escape(o.Argument1, "message")}\"");
                desc = "Stash or restore uncommitted changes.";
                break;

            case GitCommandCategory.Remote:
                sb.Append(" remote");
                if (o.Flag1) sb.Append(" -v");
                if (!string.IsNullOrWhiteSpace(o.Argument1)) sb.Append($" add {Escape(o.Argument1, "name")} {Escape(o.Argument2, "url")}");
                desc = o.Flag1 ? "List remote repositories." : "Add a remote repository.";
                break;

            case GitCommandCategory.Log:
                sb.Append(" log");
                if (o.Flag1) sb.Append(" --oneline");
                if (o.Flag2) sb.Append(" --graph");
                if (!string.IsNullOrWhiteSpace(o.Argument1)) sb.Append($" -n {Escape(o.Argument1, "10")}");
                desc = "Show commit history.";
                break;
        }

        return (sb.ToString(), desc);
    }

    private static string Escape(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return value.Trim().Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}