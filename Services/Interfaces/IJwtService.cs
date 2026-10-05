
/// <summary>
/// JWT (JSON Web Token) decode servis arayüzü. Sadece decode işlemi yapar, doğrulama (verify) yapmaz.
/// Referans: https://datatracker.ietf.org/doc/html/rfc7519
/// </summary>
public interface IJwtService
{
    string DecodePayload(string jwt);
    string DecodeHeader(string jwt);
}
