namespace tool_cantam.Services.Interfaces;

public interface IUrlService
{
    string Encode(string text);
    string Decode(string text);
}