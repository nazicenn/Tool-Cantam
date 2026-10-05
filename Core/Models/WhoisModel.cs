namespace tool_cantam.Core.Models;

public class WhoisModel
{
    public string Domain { get; set; } = string.Empty;
    public string Registrar { get; set; } = string.Empty;
    public string CreationDate { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty;
    public string UpdatedDate { get; set; } = string.Empty;
    public string NameServers { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string OwnerCountry { get; set; } = string.Empty;
    public string RawData { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}