using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class Base64ImageServiceTests
{
    private readonly Base64ImageService _service = new();

    private static string CreateTestPng()
    {
        var path = Path.Combine(Path.GetTempPath(), $"b64img_{System.Guid.NewGuid():N}.png");
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen()) dc.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(0, 0, 16, 16));
        var rtb = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(path);
        enc.Save(fs);
        return path;
    }

    [Fact]
    public async Task ImageToBase64Async_ValidPng_ReturnsBase64()
    {
        var p = CreateTestPng();
        try
        {
            var r = await _service.ImageToBase64Async(p, TestContext.Current.CancellationToken);
            Assert.True(r.IsSuccess);
            Assert.Equal("PNG", r.Value!.Format);
            Assert.NotEmpty(r.Value.Base64);
        }
        finally { File.Delete(p); }
    }

    [Fact]
    public async Task ImageToBase64Async_Empty_ReturnsValidation()
    {
        var r = await _service.ImageToBase64Async("", TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task ImageToBase64Async_Missing_ReturnsNotFound()
    {
        var r = await _service.ImageToBase64Async(@"C:\nonexistent_xyz.png", TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.NotFound, r.ErrorKind);
    }

    [Fact]
    public async Task Base64ToImageAsync_ValidBase64_ReturnsBytes()
    {
        var p = CreateTestPng();
        try
        {
            var img = await _service.ImageToBase64Async(p, TestContext.Current.CancellationToken);
            var back = await _service.Base64ToImageAsync(img.Value!.Base64, TestContext.Current.CancellationToken);
            Assert.True(back.IsSuccess);
            Assert.Equal("PNG", back.Value!.DetectedFormat);
        }
        finally { File.Delete(p); }
    }

    [Fact]
    public async Task Base64ToImageAsync_InvalidBase64_ReturnsInvalidFormat()
    {
        var r = await _service.Base64ToImageAsync("not-base64!!!", TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public async Task Base64ToImageAsync_NonImage_ReturnsNotAnImage()
    {
        var text = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hello world hello"));
        var r = await _service.Base64ToImageAsync(text, TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }
}