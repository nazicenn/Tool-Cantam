using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class ExifViewerServiceTests
{
    private readonly ExifViewerService _service = new();

    private static string CreateTestPng()
    {
        var path = Path.Combine(Path.GetTempPath(), $"exif_{System.Guid.NewGuid():N}.png");
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen()) dc.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(0, 0, 32, 32));
        var rtb = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(path);
        enc.Save(fs);
        return path;
    }

    [Fact]
    public async Task ReadAsync_ValidPng_ReturnsDirs()
    {
        var p = CreateTestPng();
        try
        {
            var r = await _service.ReadAsync(p, CancellationToken.None);
            Assert.True(r.IsSuccess);
            Assert.NotNull(r.Value);
        }
        finally { File.Delete(p); }
    }

    [Fact]
    public async Task ReadAsync_Empty_ReturnsValidation()
    {
        var r = await _service.ReadAsync("", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task ReadAsync_Missing_ReturnsNotFound()
    {
        var r = await _service.ReadAsync(@"C:\nonexistent_xyz.png", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.NotFound, r.ErrorKind);
    }

    [Fact]
    public async Task ReadAsync_UnsupportedExtension_ReturnsInvalidFormat()
    {
        var p = Path.Combine(Path.GetTempPath(), $"exif_{System.Guid.NewGuid():N}.txt");
        File.WriteAllText(p, "x");
        try
        {
            var r = await _service.ReadAsync(p, CancellationToken.None);
            Assert.False(r.IsSuccess);
            Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
        }
        finally { File.Delete(p); }
    }
}