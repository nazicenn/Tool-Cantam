using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class MetadataServiceTests
{
    private readonly MetadataService _service = new();

    private static string CreatePng(int w = 32, int h = 32)
    {
        var path = Path.Combine(Path.GetTempPath(), $"meta_{System.Guid.NewGuid():N}.png");
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
            dc.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(0, 0, w, h));

        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(path);
        encoder.Save(fs);
        return path;
    }

    [Fact]
    public async Task AnalyzeAsync_ValidImage_ReturnsOk()
    {
        var p = CreatePng();
        try
        {
            var r = await _service.AnalyzeAsync(p, CancellationToken.None);
            Assert.True(r.IsSuccess);
            Assert.NotNull(r.Value);
            Assert.Equal("PNG", r.Value!.Format);
        }
        finally { File.Delete(p); }
    }

    [Fact]
    public async Task AnalyzeAsync_NullPath_ReturnsValidation()
    {
        var r = await _service.AnalyzeAsync(null, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task AnalyzeAsync_MissingFile_ReturnsNotFound()
    {
        var r = await _service.AnalyzeAsync(@"C:\no_file_meta_xyz.png", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.NotFound, r.ErrorKind);
    }

    [Fact]
    public async Task AnalyzeAsync_UnsupportedExtension_ReturnsInvalidFormat()
    {
        var p = Path.Combine(Path.GetTempPath(), $"meta_{System.Guid.NewGuid():N}.txt");
        File.WriteAllText(p, "x");
        try
        {
            var r = await _service.AnalyzeAsync(p, CancellationToken.None);
            Assert.False(r.IsSuccess);
            Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
        }
        finally { File.Delete(p); }
    }

    [Fact]
    public async Task CleanAsync_ValidImage_ProducesOutput()
    {
        var src = CreatePng();
        var dst = Path.Combine(Path.GetTempPath(), $"meta_out_{System.Guid.NewGuid():N}.png");
        try
        {
            var r = await _service.CleanAsync(src, dst, MetadataCleanOptions.All, CancellationToken.None);
            Assert.True(r.IsSuccess);
            Assert.True(File.Exists(dst));
        }
        finally
        {
            if (File.Exists(src)) File.Delete(src);
            if (File.Exists(dst)) File.Delete(dst);
            var tmp = dst + ".tmp";
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public async Task CleanAsync_NoOptions_ReturnsValidation()
    {
        var src = CreatePng();
        try
        {
            var r = await _service.CleanAsync(src, Path.GetTempPath() + "x.png", MetadataCleanOptions.None, CancellationToken.None);
            Assert.False(r.IsSuccess);
            Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
        }
        finally { File.Delete(src); }
    }

    [Fact]
    public async Task CleanAsync_Cancelled_ReturnsFailure()
    {
        var src = CreatePng(128, 128);
        var dst = Path.Combine(Path.GetTempPath(), $"meta_out_{System.Guid.NewGuid():N}.png");
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var r = await _service.CleanAsync(src, dst, MetadataCleanOptions.All, cts.Token);
            Assert.False(r.IsSuccess);
        }
        finally
        {
            if (File.Exists(src)) File.Delete(src);
            if (File.Exists(dst)) File.Delete(dst);
        }
    }
}