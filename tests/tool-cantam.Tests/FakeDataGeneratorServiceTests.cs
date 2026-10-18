using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class FakeDataGeneratorServiceTests
{
    private readonly FakeDataGeneratorService _service = new();

    private static FakeDataOptions Opts(int count = 5, bool name = true, bool email = true, bool phone = true, bool addr = true, bool comp = true)
        => new(count, name, email, phone, addr, comp);

    [Fact]
    public async Task GenerateAsync_Valid_ReturnsJson()
    {
        var r = await _service.GenerateAsync(Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.NotNull(r.Value!.Json);
        using var doc = JsonDocument.Parse(r.Value.Json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task GenerateAsync_Count1_ReturnsObject()
    {
        var r = await _service.GenerateAsync(Opts(count: 1), CancellationToken.None);
        Assert.True(r.IsSuccess);
        using var doc = JsonDocument.Parse(r.Value!.Json);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task GenerateAsync_InvalidCount_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(count: 0), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_NoFields_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(name: false, email: false, phone: false, addr: false, comp: false), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }
}