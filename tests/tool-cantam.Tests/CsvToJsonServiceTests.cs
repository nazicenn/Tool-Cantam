using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class CsvToJsonServiceTests
{
    private readonly CsvToJsonService _service = new();
    private static CsvOptions Opts(char d = ',', bool h = true, bool t = true, bool ty = true, int i = 2) => new(d, h, t, ty, i);

    [Fact]
    public async Task ConvertAsync_Simple_Works()
    {
        var r = await _service.ConvertAsync("a,b\n1,2\n3,4", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("\"a\"", r.Value!.OutputJson);
        Assert.Contains("1", r.Value.OutputJson);
        Assert.Equal(2, r.Value.Rows);
    }

    [Fact]
    public async Task ConvertAsync_QuotedField_Works()
    {
        var r = await _service.ConvertAsync("a,b\n\"x,y\",2", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("x,y", r.Value!.OutputJson);
    }

    [Fact]
    public async Task ConvertAsync_EscapedQuote_Works()
    {
        var r = await _service.ConvertAsync("a\n\"he said \"\"hi\"\"\"", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("he said \\\"hi\\\"", r.Value!.OutputJson);
    }

    [Fact]
    public async Task ConvertAsync_Semicolon_Works()
    {
        var r = await _service.ConvertAsync("a;b\n1;2", Opts(d: ';'), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("\"b\"", r.Value!.OutputJson);
    }

    [Fact]
    public async Task ConvertAsync_DetectTypes_NumbersAsNumbers()
    {
        var r = await _service.ConvertAsync("a\n42", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("42", r.Value!.OutputJson);
        Assert.DoesNotContain("\"42\"", r.Value.OutputJson);
    }

    [Fact]
    public async Task ConvertAsync_InconsistentColumns_ReturnsError()
    {
        var r = await _service.ConvertAsync("a,b\n1,2\n3", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public async Task ConvertAsync_Empty_ReturnsValidation()
    {
        var r = await _service.ConvertAsync("", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }
}