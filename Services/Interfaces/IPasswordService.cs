namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Kriptografik güvenli parola üretim servis arayüzü.
/// Referans: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getstring
/// </summary>
public interface IPasswordService
{
    string Generate(int length, bool upper, bool lower, bool digits, bool special);
}
