using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class NsLookupServiceTests
{
    private sealed class FakeDns : IDnsService
    {
        public int QueryTimeoutMs => 5000;
        public IReadOnlyList<string> SupportedRecordTypes => new[] { "A", "AAAA", "MX", "NS", "TXT", "CNAME", "SOA" };

        public ToolResult<string> ValidateDomain(string? domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return ToolResult<string>.Fail(ToolErrorKind.Validation, "L.Dns.Error.InvalidDomain", "Empty");
            if (domain.Contains('_'))
                return ToolResult<string>.Fail(ToolErrorKind.Validation, "L.Dns.Error.InvalidDomain", "Underscore");
            if (!Uri.TryCreate($"https://{domain}", UriKind.Absolute, out var u) || u.Host != domain)
                return ToolResult<string>.Fail(ToolErrorKind.Validation, "L.Dns.Error.InvalidDomain", "Invalid");
            return ToolResult<string>.Ok(domain);
        }

        public ToolResult<DnsLookupReport>? Result { get; set; }

        public Task<ToolResult<DnsLookupReport>> LookupAsync(string? domain, CancellationToken ct = default)
            => Task.FromResult(Result ?? ToolResult<DnsLookupReport>.Ok(new DnsLookupReport(
                domain ?? "",
                TimeSpan.Zero,
                new List<DnsRecordEntry>
                {
                    new("NS", "example.com", "ns1.example.com"),
                    new("NS", "example.com", "ns2.example.com"),
                    new("A",  "example.com", "1.2.3.4"),
                })));
    }

    private static NsLookupService Create(out FakeDns dns)
    {
        dns = new FakeDns();
        return new NsLookupService(dns);
    }

    [Fact]
    public async Task LookupAsync_ValidDomain_ReturnsOnlyNs()
    {
        var svc = Create(out _);
        var r = await svc.LookupAsync("example.com", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Value!.NameServers.Count);
        Assert.Contains("ns1.example.com", r.Value.NameServers);
    }

    [Fact]
    public async Task LookupAsync_EmptyDomain_ReturnsValidation()
    {
        var svc = Create(out _);
        var r = await svc.LookupAsync("", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task LookupAsync_InvalidDomain_ReturnsValidation()
    {
        var svc = Create(out _);
        var r = await svc.LookupAsync("not_a_domain", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task LookupAsync_DnsFail_ReturnsNotFound()
    {
        var svc = Create(out var dns);
        dns.Result = ToolResult<DnsLookupReport>.Fail(ToolErrorKind.NotFound, "x", "e");
        var r = await svc.LookupAsync("example.com", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.NotFound, r.ErrorKind);
    }

    [Fact]
    public async Task LookupAsync_NoNsRecords_ReturnsEmpty()
    {
        var svc = Create(out var dns);
        dns.Result = ToolResult<DnsLookupReport>.Ok(new DnsLookupReport("x", TimeSpan.Zero,
            new List<DnsRecordEntry> { new("A", "example.com", "1.2.3.4") }));
        var r = await svc.LookupAsync("example.com", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Empty(r.Value!.NameServers);
    }
}