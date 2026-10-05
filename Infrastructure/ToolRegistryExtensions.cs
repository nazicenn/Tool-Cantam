using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;

namespace tool_cantam.Infrastructure;

public static class ToolRegistryExtensions
{
    public static void DiscoverAndRegisterTools(this IServiceCollection sc)
    {
        var registry = new ToolRegistryInstance();
        var assembly = Assembly.GetExecutingAssembly();

        // Tüm ViewModel'leri bul (Views namespace'inde olmayanlar)
        var viewModelTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.Namespace != null &&
                        t.Namespace.Contains("ViewModels") &&
                        !t.Name.Contains("Category") &&
                        !t.Name.Contains("Shell") &&
                        !t.Name.Contains("Universal") &&
                        !t.Name.Contains("MainWindow"))
            .ToList();

        foreach (var vmType in viewModelTypes)
        {
            // Karşılık gelen Page tipini bul
            var pageType = GetCorrespondingPageType(vmType, assembly);
            if (pageType == null) continue;

            // ToolDefinition oluştur
            var tool = CreateToolDefinition(vmType, pageType);
            registry.Register(tool);

            // DI'a kaydet
            sc.AddTransient(vmType);
            sc.AddTransient(pageType);
        }

        sc.AddSingleton(registry);
        sc.AddSingleton<ToolRegistryInstance>(sp => registry);
    }

    private static Type? GetCorrespondingPageType(Type vmType, Assembly assembly)
    {
        var vmName = vmType.Name;
        var pageName = vmName.Replace("ViewModel", "Page");
        var pageFullName = vmType.FullName!
            .Replace(vmName, pageName)
            .Replace(".ViewModels.", ".Views.");

        return assembly.GetType(pageFullName);
    }

    private static ToolDefinition CreateToolDefinition(Type vmType, Type pageType)
    {
        var name = GetToolName(vmType);

        return new ToolDefinition
        {
            Name = name,
            Description = GetToolDescription(name),
            Category = GetToolCategory(vmType.Name),
            ViewModelType = vmType,
            ViewType = pageType,
            Execute = async (input) => await Task.FromResult("OK")
        };
    }

    private static string GetToolName(Type vmType)
    {
        return vmType.Name.Replace("ViewModel", "") switch
        {
            "Base64" => "Base64 Codec",
            "Url" => "URL Encoder/Decoder",
            "Hex" => "Hex Encoder/Decoder",
            "HtmlEncoder" => "HTML Encoder/Decoder",
            "BinaryEncoder" => "Binary Encoder/Decoder",
            "AsciiConverter" => "ASCII Converter",
            "UnicodeConverter" => "Unicode Converter",
            "Rot13" => "ROT13 Cipher",
            "CaseConverter" => "Case Converter",
            "TextCounter" => "Text Counter",
            "StringReverser" => "String Reverser",
            "JsonFormatter" => "JSON Formatter",
            "Hash" => "Hash Generator",
            "HashIdentifier" => "Hash Identifier",
            "HmacGenerator" => "HMAC Generator",
            "Jwt" => "JWT Decoder",
            "Password" => "Password Generator",
            "Metadata" => "Metadata Cleaner",
            "PortScanner" => "Port Scanner",
            "Subdomain" => "Subdomain Scanner",
            "Dns" => "DNS Lookup",
            "ReverseDns" => "Reverse DNS",
            "IpInfo" => "IP Info",
            "Whois" => "WHOIS Lookup",
            "MacLookup" => "MAC Lookup",
            "SslChecker" => "SSL Checker",
            "Ping" => "Ping Test",
            "Pwned" => "Leak Checker",
            "UnixTimestamp" => "Unix Timestamp",
            "QrCode" => "QR Code Generator",
            "TextSorter" => "Text Sorter",
            "DuplicateLineRemover" => "Duplicate Line Remover",
            "UuidGenerator" => "UUID Generator",
            "RandomStringGenerator" => "Random String Generator",
            "SlugGenerator" => "Slug Generator",
            "FindReplace" => "Find and Replace",
            "RegexTester" => "Regex Tester",
            "TextDiff" => "Text Diff",
            "SqlFormatter" => "SQL Formatter",
            "CsvToJson" => "CSV to JSON",
            "ColorPicker" => "Color Picker",
            "LoremIpsum" => "Lorem Ipsum Generator",
            "Markdown" => "Markdown Viewer",
            "QrCodeReader" => "QR Code Reader",
            "JwtGenerator" => "JWT Generator",
            "CronParser" => "Cron Parser",
            "XmlFormatter" => "XML Formatter",
            "AesEncrypt" => "AES Encrypt/Decrypt",
            "RsaKeyGenerator" => "RSA Key Generator",
            "HttpRequestTester" => "HTTP Request Tester",
            "BcryptGenerator" => "Bcrypt Generator",
            "IpCalculator" => "IP Calculator",
            "ExifViewer" => "EXIF Viewer",
            "CurlGenerator" => "cURL Generator",
            "SecretGenerator" => "Secret Generator",
            "GeoIpLookup" => "GeoIP Lookup",
            "YamlFormatter" => "YAML Formatter",
            "JsonMinifier" => "JSON Minifier",
            "FakeDataGenerator" => "Fake Data Generator",
            "CssGradientGenerator" => "CSS Gradient Generator",
            "MxLookup" => "MX Lookup",
            "NsLookup" => "NS Lookup",
            "CertificateDecoder" => "Certificate Decoder",
            "PemViewer" => "PEM Viewer",
            "Traceroute" => "Traceroute",
            "Base64ImageConverter" => "Base64 Image Converter", 
            var name => name
        };
    }

    private static string GetToolDescription(string toolName)
    {
        return toolName switch
        {
            "Base64 Codec" => "Base64 encoding and decoding operations",
            "URL Encoder/Decoder" => "Encode/Decode URL strings",
            "Hex Encoder/Decoder" => "Convert text to hexadecimal",
            "HTML Encoder/Decoder" => "Encode and decode HTML entities",
            "Binary Encoder/Decoder" => "Convert text to binary",
            "ROT13 Cipher" => "Simple letter substitution cipher",
            "ASCII Converter" => "Convert text to ASCII codes",
            "Unicode Converter" => "Convert text to Unicode escape sequences",
            "Case Converter" => "Convert text case",
            "Text Counter" => "Count characters, words, lines",
            "String Reverser" => "Reverse any text",
            "JSON Formatter" => "Format and beautify JSON data",
            "Hash Generator" => "MD5, SHA1, SHA256 hash generator",
            "Hash Identifier" => "Detect hash algorithm type",
            "HMAC Generator" => "Generate HMAC-SHA256 hash",
            "JWT Decoder" => "JSON Web Token decoding",
            "Password Generator" => "Secure password generation",
            "Metadata Cleaner" => "EXIF and metadata removal",
            "Port Scanner" => "TCP port scanning tool",
            "Subdomain Scanner" => "Find subdomains of a target domain",
            "DNS Lookup" => "Query DNS records",
            "Reverse DNS" => "Get hostname from IP address",
            "IP Info" => "Get geolocation and ISP information",
            "WHOIS Lookup" => "Query domain registration information",
            "MAC Lookup" => "Find vendor from MAC address",
            "SSL Checker" => "Check SSL/TLS certificate details",
            "Ping Test" => "Test network latency",
            "Unix Timestamp" => "Convert between timestamp and date",
            "QR Code Generator" => "Generate QR codes",
            "Text Sorter" => "Sort lines alphabetically in ascending or descending order",
            "Duplicate Line Remover" => "Remove duplicate lines from text",
            "UUID Generator" => "Generate UUID/GUID identifiers",
            "Random String Generator" => "Generate random strings for passwords and testing",
            "Slug Generator" => "Generate URL-friendly slug from text",
            "Find and Replace" => "Find and replace text in strings",
            "Regex Tester" => "Test regular expressions and find matches",
            "Text Diff" => "Compare two texts and show differences",
            "SQL Formatter" => "Format and beautify SQL queries",
            "XML Formatter" => "Format and validate XML documents",
            "CSV to JSON" => "Convert CSV to JSON format",
            "Color Picker" => "Pick and convert colors (HEX, RGB, HSL)",
            "Lorem Ipsum Generator" => "Generate dummy text for design and testing",
            "QR Code Reader" => "Read QR codes from images",
            "Markdown Viewer" => "Preview Markdown as HTML",
            "JWT Generator" => "Generate JSON Web Tokens",
            "Cron Parser" => "Parse and explain cron expressions",
            "AES Encrypt/Decrypt" => "Encrypt and decrypt text using AES-256",
            "RSA Key Generator" => "Generate RSA public/private key pairs",
            "HTTP Request Tester" => "Test HTTP requests like a mini Postman",
            "Bcrypt Generator" => "Generate and verify Bcrypt password hashes",
            "IP Calculator" => "Calculate subnet details for IP addresses",
            "EXIF Viewer" => "View EXIF metadata from images",
            "cURL Generator" => "Generate cURL commands from API requests",
            "Secret Generator" => "Generate API keys, JWT secrets, tokens",
            "GeoIP Lookup" => "Get geolocation information for IP addresses",
            "YAML Formatter" => "Format and beautify YAML documents",
            "JSON Minifier" => "Minify and format JSON data",
            "Fake Data Generator" => "Generate fake test data",
            "CSS Gradient Generator" => "Generate CSS linear gradients",
            "MX Lookup" => "Look up MX records for a domain",
            "NS Lookup" => "Look up name servers for a domain",
            "Certificate Decoder" => "Decode and inspect X.509 certificates",
            "PEM Viewer" => "View and parse PEM certificates",
            "Traceroute" => "Trace network route to a host",
            "Base64 Image Converter" => "Convert images to Base64 strings and vice versa", 
            _ => toolName
        };
    }

    private static ToolCategory GetToolCategory(string typeName)
    {
        if (typeName.Contains("Encoder") || typeName.Contains("Decoder") || typeName.Contains("Converter") || typeName.Contains("Cipher") || typeName.Contains("Hex") || typeName.Contains("Rot13") || typeName.Contains("Ascii") || typeName.Contains("Unicode") || typeName.Contains("Binary") || typeName.Contains("Html") || typeName.Contains("Base64Image"))
            return ToolCategory.EncodersDecoders;

        if (typeName.Contains("Hash") || typeName.Contains("Hmac") || typeName.Contains("Identifier") || typeName.Contains("Aes") || typeName.Contains("Rsa") || typeName.Contains("Bcrypt"))
            return ToolCategory.HashAndCrypto;

        if (typeName.Contains("Scanner") || typeName.Contains("Lookup") || typeName.Contains("Ping") || typeName.Contains("Dns") || typeName.Contains("Ip") || typeName.Contains("Whois") || typeName.Contains("Mac") || typeName.Contains("Ssl") || typeName.Contains("Port") || typeName.Contains("IpCalculator"))
            return ToolCategory.NetworkScanner;

        if (typeName.Contains("Case") || typeName.Contains("Counter") || typeName.Contains("String") || typeName.Contains("Sorter") || typeName.Contains("Duplicate") || typeName.Contains("Slug") || typeName.Contains("Find") || typeName.Contains("Diff") || typeName.Contains("Lorem") || typeName.Contains("Ipsum"))
            return ToolCategory.TextAndString;

        if (typeName.Contains("Json") || typeName.Contains("Uuid") || typeName.Contains("RandomString") || typeName.Contains("Regex") || typeName.Contains("Sql") || typeName.Contains("Xml") || typeName.Contains("Csv") || typeName.Contains("Color") || typeName.Contains("Json") || typeName.Contains("Markdown"))
            return ToolCategory.DeveloperTools;

        if (typeName.Contains("Jwt") || typeName.Contains("Jwt"))
            return ToolCategory.Encryption;

        if (typeName.Contains("Metadata") || typeName.Contains("Exif"))
            return ToolCategory.FileAndMetadata;

        if (typeName.Contains("Password") || typeName.Contains("Pwned"))
            return ToolCategory.PasswordCredential;

        if (typeName.Contains("Qr") || typeName.Contains("QrCode") || typeName.Contains("HttpRequest"))
            return ToolCategory.ApiWeb;

        if (typeName.Contains("Unix") || typeName.Contains("Cron"))
            return ToolCategory.DateTime;
        if (typeName.Contains("HttpHeader") || typeName.Contains("Curl") || typeName.Contains("FakeData"))
            return ToolCategory.ApiWeb;

        if (typeName.Contains("PasswordStrength") || typeName.Contains("Secret"))
            return ToolCategory.PasswordCredential;

        if (typeName.Contains("GeoIp"))
            return ToolCategory.NetworkScanner;

        if (typeName.Contains("Yaml") || typeName.Contains("JsonMinifier") || typeName.Contains("XmlMinifier") || typeName.Contains("CssGradient"))
            return ToolCategory.DeveloperTools;

        if (typeName.Contains("Mx") || typeName.Contains("Ns"))
            return ToolCategory.NetworkScanner;

        if (typeName.Contains("CertificateDecoder") || typeName.Contains("PemViewer"))
            return ToolCategory.Encryption;
        if (typeName.Contains("Traceroute"))
            return ToolCategory.NetworkScanner;

        return ToolCategory.DeveloperTools;
    }
}