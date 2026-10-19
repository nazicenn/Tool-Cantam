using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class YamlToJsonServiceTests
{
    private readonly YamlToJsonService _service = new();
    private static YamlToJsonOptions Opts(bool indent = true) => new(indent);

    [Fact] public async Task ConvertAsync_Simple_Works() { var r = await _service.ConvertAsync("name: John", Opts(), TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Contains("\"name\"", r.Value!.Json); }
    [Fact] public async Task ConvertAsync_Nested_Works() { var r = await _service.ConvertAsync("user:\n  name: John", Opts(), TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Contains("user", r.Value!.Json); }
    [Fact] public async Task ConvertAsync_Empty_ReturnsValidation() { var r = await _service.ConvertAsync("", Opts(), TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
    [Fact] public async Task ConvertAsync_InvalidYaml_ReturnsInvalidFormat() { var r = await _service.ConvertAsync("- a\n: b", Opts(), TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); }
}