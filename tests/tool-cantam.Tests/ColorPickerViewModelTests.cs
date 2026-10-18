using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace tool_cantam.Tests;

public sealed class ColorPickerViewModelTests
{
    private sealed class FakeSvc : IColorPickerService
    {
        public int MaxHexLength => 9;
        public ToolResult<ColorPickerReport>? Result { get; set; }
        public Task<ToolResult<ColorPickerReport>> FromHexAsync(string? h, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<ColorPickerReport>.Ok(Sample(255, 0, 0)));
        public Task<ToolResult<ColorPickerReport>> FromRgbAsync(int r, int g, int b, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<ColorPickerReport>.Ok(Sample(r, g, b)));
        public Task<ToolResult<ColorPickerReport>> RandomAsync(CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<ColorPickerReport>.Ok(Sample(128, 128, 128)));
        private static ColorPickerReport Sample(int r, int g, int b)
            => new(r, g, b, $"#{r:X2}{g:X2}{b:X2}", $"rgb({r}, {g}, {b})", "hsl(0, 0%, 50%)", 0, 0, 50, false, TimeSpan.Zero);
    }
    private sealed class FakeFe : IFileExportService
    {
        public string? LastCopied { get; private set; }
        public string? SavePath { get; set; }
        public string? RequestOpenPath(string f, string t = "Select File") => null;
        public string? RequestSavePath(string f, string e, string n = "") => SavePath;
        public Task<bool> ExportTextAsync(string c, string f, string e, string n = "Export") => Task.FromResult(true);
        public void CopyToClipboard(string c) => LastCopied = c;
    }
    private sealed class FakeLoc : ILocalizerService
    {
        private string _c = "tr-TR";
        public string CurrentCulture => _c;
        public IReadOnlyCollection<string> AvailableCultures => new[] { "tr-TR", "en-US" };
        public string Get(string k) => k;
        public string Format(string k, params object?[] a) => a is { Length: > 0 } ? $"{k} [{string.Join(", ", a)}]" : k;
        public bool SetCulture(string c) { _c = c; return true; }
    }
    private static ColorPickerViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new ColorPickerViewModel(svc, fe, new FakeLoc(), NullLogger<ColorPickerViewModel>.Instance);
    }

    [Fact] public void Limits() { var vm = Create(out _, out _); Assert.Contains("9", vm.LimitsText); }
    [Fact] public async Task ApplyHex_Valid_Works() { var vm = Create(out _, out _); vm.HexColor = "#FF0000"; await vm.ApplyHexCommand.ExecuteAsync(null); Assert.Equal(255, vm.Red); }
    [Fact] public async Task Random_Works() { var vm = Create(out _, out _); await vm.RandomColorCommand.ExecuteAsync(null); Assert.NotNull(vm.HexColor); }
    [Fact] public void CopyHex_Works() { var vm = Create(out _, out var fe); vm.HexColor = "#123456"; vm.CopyHexCommand.Execute(null); Assert.Equal("#123456", fe.LastCopied); }
    [Fact] public void Clear_Resets() { var vm = Create(out _, out _); vm.Red = 100; vm.ClearCommand.Execute(null); Assert.Equal(30, vm.Red); }
}