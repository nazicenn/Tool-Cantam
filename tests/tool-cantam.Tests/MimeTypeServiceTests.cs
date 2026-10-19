using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class MimeTypeServiceTests
{
    private readonly MimeTypeService _service = new();

    [Fact] public async Task LookupByExtension_Txt_ReturnsText() { var r = await _service.LookupByExtensionAsync("txt", TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Equal("text/plain", r.Value!.MimeType); }
    [Fact] public async Task LookupByExtension_WithDot_Works() { var r = await _service.LookupByExtensionAsync(".png", TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Equal("image/png", r.Value!.MimeType); }
    [Fact] public async Task LookupByExtension_Unknown_ReturnsNotFound() { var r = await _service.LookupByExtensionAsync("xyz123", TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.NotFound, r.ErrorKind); }
    [Fact] public async Task LookupByMime_TextPlain_ReturnsTxt() { var r = await _service.LookupByMimeAsync("text/plain", TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Equal("text/plain", r.Value!.MimeType); }
    [Fact] public async Task LookupByMime_Unknown_ReturnsNotFound() { var r = await _service.LookupByMimeAsync("unknown/type", TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.NotFound, r.ErrorKind); }
    [Fact] public async Task LookupByExtension_Empty_ReturnsValidation() { var r = await _service.LookupByExtensionAsync("", TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
    [Fact] public async Task LookupByExtension_CaseInsensitive_Works() { var r = await _service.LookupByExtensionAsync("TXT", TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); }
    [Fact] public void AllExtensions_NotEmpty() { Assert.NotEmpty(_service.AllExtensions); }
}