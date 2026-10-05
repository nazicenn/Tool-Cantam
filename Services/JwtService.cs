using System;
using System.Text;

namespace tool_cantam.Services;

/// <summary>
/// JWT (JSON Web Token) decode servis uygulaması.
/// Header ve Payload parçalarını Base64Url formatından çözümler.
/// </summary>
public class JwtService : IJwtService
{
    public string DecodePayload(string jwt) => DecodePart(jwt, 1);
    public string DecodeHeader(string jwt) => DecodePart(jwt, 0);

    private static string DecodePart(string jwt, int partIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwt);

        var parts = jwt.Trim().Split('.');
        if (parts.Length < 2 || parts.Length > 3)
            throw new FormatException("Invalid JWT format. JWT must consist of 2 or 3 parts (header.payload[.signature]).");

        // Index sınır kontrolü
        if (partIndex < 0 || partIndex >= parts.Length)
            throw new ArgumentOutOfRangeException(nameof(partIndex), $"Invalid JWT part index: {partIndex}");

        var base64Url = parts[partIndex];
        var padLength = 4 - (base64Url.Length % 4);
        if (padLength < 4)
        {
            base64Url += new string('=', padLength);
        }
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }
}
