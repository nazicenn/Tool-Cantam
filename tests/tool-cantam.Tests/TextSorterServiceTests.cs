using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class TextSorterServiceTests
{
    private readonly TextSorterService _service = new();

    private static TextSorterOptions Opts(
        SortDirection d = SortDirection.Ascending,
        SortMode m = SortMode.Invariant,
        bool ignoreCase = true,
        bool removeEmpty = false,
        bool trim = false,
        bool removeDup = false)
        => new(d, m, ignoreCase, removeEmpty, trim, removeDup);

    [Fact]
    public async Task SortAsync_Ascending_SortsLines()
    {
        var r = await _service.SortAsync("banana\napple\ncherry", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("apple\nbanana\ncherry", r.Value!.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task SortAsync_Descending_SortsLines()
    {
        var r = await _service.SortAsync("apple\nbanana\ncherry",
            Opts(SortDirection.Descending), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("cherry\nbanana\napple", r.Value!.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task SortAsync_IgnoreCase_True()
    {
        var r = await _service.SortAsync("B\nA\nc", Opts(ignoreCase: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("A\nB\nc", r.Value!.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task SortAsync_OrdinalMode_SortsAscii()
    {
        var r = await _service.SortAsync("B\na", Opts(m: SortMode.Ordinal, ignoreCase: false), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("B\na", r.Value!.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task SortAsync_RemoveEmptyLines_RemovesBlanks()
    {
        var r = await _service.SortAsync("a\n\nb\n\nc", Opts(removeEmpty: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Value!.RemovedEmpty);
        Assert.Equal(3, r.Value!.OutputLines);
    }

    [Fact]
    public async Task SortAsync_RemoveDuplicates_RemovesDups()
    {
        var r = await _service.SortAsync("a\na\nb", Opts(removeDup: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(1, r.Value!.RemovedDuplicates);
    }

    [Fact]
    public async Task SortAsync_Empty_ReturnsValidation()
    {
        var r = await _service.SortAsync("", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }
}