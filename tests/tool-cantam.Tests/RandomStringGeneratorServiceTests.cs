using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class RandomStringGeneratorServiceTests
{
    private readonly RandomStringGeneratorService _service = new();

    private static RandomStringOptions Opts(
        int length = 12, int count = 1,
        bool upper = true, bool lower = true, bool digits = true, bool symbols = false,
        bool excludeSimilar = false, bool ensureEach = false)
        => new(length, count, upper, lower, digits, symbols, excludeSimilar, ensureEach);

    [Fact]
    public async Task GenerateAsync_Valid_ReturnsCountAndLength()
    {
        var r = await _service.GenerateAsync(Opts(16, 3), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Value!.Items.Count);
        Assert.All(r.Value.Items, s => Assert.Equal(16, s.Length));
    }

    [Fact]
    public async Task GenerateAsync_InvalidLength_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(2), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_InvalidCount_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(12, 0), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_NoCharacterSet_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(
            Opts(12, 1, upper: false, lower: false, digits: false, symbols: false),
            CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_OnlyUppercase_NoLowerInResult()
    {
        var r = await _service.GenerateAsync(
            Opts(20, 1, upper: true, lower: false, digits: false, symbols: false),
            CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.All(r.Value!.Items[0], c => Assert.True(char.IsUpper(c)));
    }

    [Fact]
    public async Task GenerateAsync_EnsureEachType_ContainsAllCategories()
    {
        var r = await _service.GenerateAsync(
            Opts(8, 5, upper: true, lower: true, digits: true, symbols: false, ensureEach: true),
            CancellationToken.None);
        Assert.True(r.IsSuccess);
        foreach (var s in r.Value!.Items)
        {
            Assert.Contains(s, char.IsUpper);
            Assert.Contains(s, char.IsLower);
            Assert.Contains(s, char.IsDigit);
        }
    }

    [Fact]
    public async Task GenerateAsync_ExcludeSimilar_NoAmbiguousChars()
    {
        var r = await _service.GenerateAsync(
            Opts(50, 5, upper: true, lower: true, digits: true, excludeSimilar: true),
            CancellationToken.None);
        Assert.True(r.IsSuccess);
        string[] bad = { "I", "l", "1", "O", "0", "o" };
        foreach (var s in r.Value!.Items)
            Assert.DoesNotContain(s, c => bad.Contains(c.ToString()));
    }

    [Fact]
    public async Task GenerateAsync_EnsureEach_LengthSmallerThanPools_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(
            Opts(2, 1, upper: true, lower: true, digits: true, symbols: true, ensureEach: true),
            CancellationToken.None);
        Assert.False(r.IsSuccess);
    }

    [Fact]
    public async Task GenerateAsync_Cancelled_ReturnsFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await _service.GenerateAsync(Opts(100, 50), cts.Token);
        Assert.False(r.IsSuccess);
    }
}