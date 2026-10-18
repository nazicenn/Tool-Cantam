using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class ColorPickerServiceTests
{
    private readonly ColorPickerService _service = new();

    [Fact] public async Task FromHexAsync_Valid_ReturnsRgb() { var r = await _service.FromHexAsync("#FF0000", TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Equal(255, r.Value!.Red); Assert.Equal("rgb(255, 0, 0)", r.Value.Rgb); }
    [Fact] public async Task FromHexAsync_Short_Expands() { var r = await _service.FromHexAsync("#F00", TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Equal(255, r.Value!.Red); }
    [Fact] public async Task FromHexAsync_Invalid_ReturnsValidation() { var r = await _service.FromHexAsync("not-hex", TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
    [Fact] public async Task FromRgbAsync_Valid_ReturnsHex() { var r = await _service.FromRgbAsync(255, 0, 0, TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Equal("#FF0000", r.Value!.Hex); }
    [Fact] public async Task FromRgbAsync_OutOfRange_ReturnsValidation() { var r = await _service.FromRgbAsync(300, 0, 0, TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
    [Fact] public async Task RandomAsync_ReturnsValid() { var r = await _service.RandomAsync(TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.InRange(r.Value!.Red, 0, 255); }
}