using System;

namespace tool_cantam.Core.Models;

public class SslCertificateInfo
{
    public string Domain { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string SignatureAlgorithm { get; set; } = string.Empty;
    public int RemainingDays { get; set; }
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}