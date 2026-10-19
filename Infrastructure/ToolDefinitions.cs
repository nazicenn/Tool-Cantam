using System.Text;
using System.Web;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.ViewModels;
using tool_cantam.Views;

namespace tool_cantam.Infrastructure;

public static class ToolDefinitions
{
    public static void RegisterAll()
    {
        ToolRegistry.Clear();

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Base64 Codec",
            Description = "Base64 encoding and decoding operations",
            Category = ToolCategory.EncodersDecoders,
            ViewModelType = typeof(Base64ViewModel),
            ViewType = typeof(Base64Page),
            Execute = async (input) => Convert.ToBase64String(Encoding.UTF8.GetBytes(input?.ToString() ?? string.Empty))
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "URL Encoder/Decoder",
            Description = "Encode/Decode URL strings for web security testing",
            Category = ToolCategory.EncodersDecoders,
            ViewModelType = typeof(UrlViewModel),
            ViewType = typeof(UrlPage),
            Execute = async (input) => Uri.EscapeDataString(input?.ToString() ?? string.Empty)
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Hex Encoder/Decoder",
            Description = "Convert text to hexadecimal and vice versa",
            Category = ToolCategory.EncodersDecoders,
            ViewModelType = typeof(HexViewModel),
            ViewType = typeof(HexPage),
            Execute = async (input) => Convert.ToHexString(Encoding.UTF8.GetBytes(input?.ToString() ?? string.Empty))
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "HTML Encoder/Decoder",
            Description = "Encode and decode HTML entities",
            Category = ToolCategory.EncodersDecoders,
            ViewModelType = typeof(HtmlEncoderViewModel),
            ViewType = typeof(HtmlEncoderPage),
            Execute = async (input) => HttpUtility.HtmlEncode(input?.ToString() ?? string.Empty)
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Binary Encoder/Decoder",
            Description = "Convert text to binary (0101) and vice versa",
            Category = ToolCategory.EncodersDecoders,
            ViewModelType = typeof(BinaryEncoderViewModel),
            ViewType = typeof(BinaryEncoderPage),
            Execute = async (input) =>
            {
                var text = input?.ToString() ?? string.Empty;
                var bytes = Encoding.UTF8.GetBytes(text);
                var binary = new StringBuilder();
                foreach (var b in bytes)
                    binary.Append(Convert.ToString(b, 2).PadLeft(8, '0')).Append(" ");
                return binary.ToString().Trim();
            }
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "ROT13 Transform",
            Description = "Letter rotation (ROT-N, default ROT13). Obfuscation only, not encryption",
            Category = ToolCategory.EncodersDecoders,
            ViewModelType = typeof(Rot13ViewModel),
            ViewType = typeof(Rot13Page)
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Character Code Converter",
            Description = "Convert between text and Unicode code points (decimal or hexadecimal); ASCII is a subset",
            Category = ToolCategory.EncodersDecoders,
            ViewModelType = typeof(AsciiConverterViewModel),
            ViewType = typeof(AsciiConverterPage)
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Unicode Converter",
            Description = "Convert text to and from Unicode escapes (\\uXXXX, \\UXXXXXXXX, \\xXX, U+XXXX)",
            Category = ToolCategory.EncodersDecoders,
            ViewModelType = typeof(UnicodeConverterViewModel),
            ViewType = typeof(UnicodeConverterPage)
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Hash Generator",
            Description = "Compute MD5, SHA-1, SHA-2 and SHA-3 digests for text or files, and verify a known digest",
            Category = ToolCategory.HashAndCrypto,
            ViewModelType = typeof(HashViewModel),
            ViewType = typeof(HashPage)
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Hash Identifier",
            Description = "Suggest candidate hash algorithms from length, character set and known prefixes",
            Category = ToolCategory.HashAndCrypto,
            ViewModelType = typeof(HashIdentifierViewModel),
            ViewType = typeof(HashIdentifierPage)
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "HMAC Generator",
            Description = "Compute and verify HMAC with SHA-2/SHA-3 and a text or hex key",
            Category = ToolCategory.HashAndCrypto,
            ViewModelType = typeof(HmacGeneratorViewModel),
            ViewType = typeof(HmacGeneratorPage)
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Port Scanner",
            Description = "TCP connect scan with per-port timeouts, progress, cancellation and service names",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(PortScannerViewModel),
            ViewType = typeof(PortScannerPage)
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Subdomain Scanner",
            Description = "Find subdomains of a target domain",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(SubdomainViewModel),
            ViewType = typeof(SubdomainPage),
            Execute = async (input) => "Subdomain list"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "DNS Lookup",
            Description = "Query DNS records for a domain",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(DnsViewModel),
            ViewType = typeof(DnsPage),
            Execute = async (input) => "DNS records"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Reverse DNS",
            Description = "Get hostname from IP address",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(ReverseDnsViewModel),
            ViewType = typeof(ReverseDnsPage),
            Execute = async (input) => "Hostname"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "IP Info",
            Description = "Get geolocation and ISP information",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(IpInfoViewModel),
            ViewType = typeof(IpInfoPage),
            Execute = async (input) => "IP geolocation info"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "WHOIS Lookup",
            Description = "Query domain registration information",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(WhoisViewModel),
            ViewType = typeof(WhoisPage),
            Execute = async (input) => "WHOIS records"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "MAC Lookup",
            Description = "Find vendor/manufacturer from MAC address",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(MacLookupViewModel),
            ViewType = typeof(MacLookupPage),
            Execute = async (input) => "Vendor name"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "SSL Checker",
            Description = "Check SSL/TLS certificate details",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(SslCheckerViewModel),
            ViewType = typeof(SslCheckerPage),
            Execute = async (input) => "Certificate info"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Ping Test",
            Description = "Test network latency",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(PingViewModel),
            ViewType = typeof(PingPage),
            Execute = async (input) => "Ping results"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Case Converter",
            Description = "Convert text case",
            Category = ToolCategory.TextAndString,
            ViewModelType = typeof(CaseConverterViewModel),
            ViewType = typeof(CaseConverterPage),
            Execute = async (input) => input?.ToString()?.ToUpper() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Text Counter",
            Description = "Count characters, words, lines",
            Category = ToolCategory.TextAndString,
            ViewModelType = typeof(TextCounterViewModel),
            ViewType = typeof(TextCounterPage),
            Execute = async (input) => "Count results"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "String Reverser",
            Description = "Reverse any text or string",
            Category = ToolCategory.TextAndString,
            ViewModelType = typeof(StringReverserViewModel),
            ViewType = typeof(StringReverserPage),
            Execute = async (input) =>
            {
                var text = input?.ToString() ?? string.Empty;
                var chars = text.ToCharArray();
                Array.Reverse(chars);
                return new string(chars);
            }
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "JSON Formatter",
            Description = "Format, validate and beautify JSON data",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(JsonFormatterViewModel),
            ViewType = typeof(JsonFormatterPage),
            Execute = async (input) => "Formatted JSON"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "JWT Decoder",
            Description = "JSON Web Token decoding",
            Category = ToolCategory.Encryption,
            ViewModelType = typeof(JwtViewModel),
            ViewType = typeof(JwtPage),
            Execute = async (input) => "Decoded JWT"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Password Generator",
            Description = "Secure password generation",
            Category = ToolCategory.PasswordCredential,
            ViewModelType = typeof(PasswordViewModel),
            ViewType = typeof(PasswordPage),
            Execute = async (input) => "Generated password"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Metadata Cleaner",
            Description = "EXIF and metadata removal",
            Category = ToolCategory.FileAndMetadata,
            ViewModelType = typeof(MetadataViewModel),
            ViewType = typeof(MetadataPage),
            Execute = async (input) => "Metadata cleaned"
        });


        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Unix Timestamp",
            Description = "Convert between Unix timestamp and date",
            Category = ToolCategory.DateTime,
            ViewModelType = typeof(UnixTimestampViewModel),
            ViewType = typeof(UnixTimestampPage),
            Execute = async (input) => "Timestamp converted"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "QR Code Generator",
            Description = "Generate QR codes",
            Category = ToolCategory.ApiWeb,
            ViewModelType = typeof(QrCodeViewModel),
            ViewType = typeof(QrCodePage),
            Execute = async (input) => "QR code generated"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "QR Code Reader",
            Description = "Decode QR codes from image files",
            Category = ToolCategory.ApiWeb,
            ViewModelType = typeof(QrCodeReaderViewModel),
            ViewType = typeof(QrCodeReaderPage)
        });

        // ==================== NETWORK & SCANNER ====================
        ToolRegistry.Register(new ToolDefinition
        {
            Name = "HTTP Header Analyzer",
            Description = "Analyze HTTP headers of any website",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(HttpHeaderViewModel),
            ViewType = typeof(HttpHeaderPage),
            Execute = async (input) => "Header analysis"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Text Sorter",
            Description = "Sort lines alphabetically in ascending or descending order",
            Category = ToolCategory.TextAndString,
            ViewModelType = typeof(TextSorterViewModel),
            ViewType = typeof(TextSorterPage),
            Execute = async (input) => "Sorted text"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "UUID Generator",
            Description = "Generate UUID/GUID identifiers",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(UuidGeneratorViewModel),
            ViewType = typeof(UuidGeneratorPage),
            Execute = async (input) => Guid.NewGuid().ToString()
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Random String Generator",
            Description = "Generate random strings for passwords and testing",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(RandomStringGeneratorViewModel),
            ViewType = typeof(RandomStringGeneratorPage),
            Execute = async (input) => Guid.NewGuid().ToString()
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Slug Generator",
            Description = "Generate URL-friendly slug from text",
            Category = ToolCategory.TextAndString,
            ViewModelType = typeof(SlugGeneratorViewModel),
            ViewType = typeof(SlugGeneratorPage),
            Execute = async (input) => input?.ToString()?.ToLower().Replace(" ", "-") ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Find and Replace",
            Description = "Find and replace text in strings",
            Category = ToolCategory.TextAndString,
            ViewModelType = typeof(FindReplaceViewModel),
            ViewType = typeof(FindReplacePage),
            Execute = async (input) => input?.ToString()?.Replace("old", "new") ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Regex Tester",
            Description = "Test regular expressions and find matches",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(RegexTesterViewModel),
            ViewType = typeof(RegexTesterPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Text Diff",
            Description = "Compare two texts and show differences",
            Category = ToolCategory.TextAndString,
            ViewModelType = typeof(TextDiffViewModel),
            ViewType = typeof(TextDiffPage),
            Execute = async (input) => "Diff result"
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "SQL Formatter",
            Description = "Format and beautify SQL queries",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(SqlFormatterViewModel),
            ViewType = typeof(SqlFormatterPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        // XML Formatter
        ToolRegistry.Register(new ToolDefinition
        {
            Name = "XML Formatter",
            Description = "Format and validate XML documents",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(XmlFormatterViewModel),
            ViewType = typeof(XmlFormatterPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });


        ToolRegistry.Register(new ToolDefinition
        {
            Name = "CSV to JSON",
            Description = "Convert CSV to JSON format",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(CsvToJsonViewModel),
            ViewType = typeof(CsvToJsonPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });


        ToolRegistry.Register(new ToolDefinition
        {
            Name = "JWT Generator",
            Description = "Generate JSON Web Tokens",
            Category = ToolCategory.Encryption,
            ViewModelType = typeof(JwtGeneratorViewModel),
            ViewType = typeof(JwtGeneratorPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Cron Parser",
            Description = "Parse and explain cron expressions",
            Category = ToolCategory.DateTime,
            ViewModelType = typeof(CronParserViewModel),
            ViewType = typeof(CronParserPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "AES Encrypt/Decrypt",
            Description = "Encrypt and decrypt text using AES-256",
            Category = ToolCategory.HashAndCrypto,
            ViewModelType = typeof(AesEncryptViewModel),
            ViewType = typeof(AesEncryptPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "RSA Key Generator",
            Description = "Generate RSA public/private key pairs",
            Category = ToolCategory.HashAndCrypto,
            ViewModelType = typeof(RsaKeyGeneratorViewModel),
            ViewType = typeof(RsaKeyGeneratorPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "HTTP Request Tester",
            Description = "Test HTTP requests like a mini Postman",
            Category = ToolCategory.ApiWeb,
            ViewModelType = typeof(HttpRequestTesterViewModel),
            ViewType = typeof(HttpRequestTesterPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Bcrypt Generator",
            Description = "Generate and verify Bcrypt password hashes",
            Category = ToolCategory.HashAndCrypto,
            ViewModelType = typeof(BcryptGeneratorViewModel),
            ViewType = typeof(BcryptGeneratorPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "IP Calculator",
            Description = "Calculate subnet details for IP addresses",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(IpCalculatorViewModel),
            ViewType = typeof(IpCalculatorPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "EXIF Viewer",
            Description = "View EXIF metadata from images",
            Category = ToolCategory.FileAndMetadata,
            ViewModelType = typeof(ExifViewerViewModel),
            ViewType = typeof(ExifViewerPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });


        ToolRegistry.Register(new ToolDefinition
        {
            Name = "cURL Generator",
            Description = "Generate cURL commands from API requests",
            Category = ToolCategory.ApiWeb,
            ViewModelType = typeof(CurlGeneratorViewModel),
            ViewType = typeof(CurlGeneratorPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });


        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Secret Generator",
            Description = "Generate API keys, JWT secrets, tokens",
            Category = ToolCategory.PasswordCredential,
            ViewModelType = typeof(SecretGeneratorViewModel),
            ViewType = typeof(SecretGeneratorPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "GeoIP Lookup",
            Description = "Get geolocation information for IP addresses",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(GeoIpLookupViewModel),
            ViewType = typeof(GeoIpLookupPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "YAML Formatter",
            Description = "Format and beautify YAML documents",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(YamlFormatterViewModel),
            ViewType = typeof(YamlFormatterPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "JSON Minifier",
            Description = "Minify and format JSON data",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(JsonMinifierViewModel),
            ViewType = typeof(JsonMinifierPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });


        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Fake Data Generator",
            Description = "Generate fake test data",
            Category = ToolCategory.ApiWeb,
            ViewModelType = typeof(FakeDataGeneratorViewModel),
            ViewType = typeof(FakeDataGeneratorPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "CSS Gradient Generator",
            Description = "Generate CSS linear gradients",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(CssGradientGeneratorViewModel),
            ViewType = typeof(CssGradientGeneratorPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "MX Lookup",
            Description = "Look up MX records for a domain",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(MxLookupViewModel),
            ViewType = typeof(MxLookupPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "NS Lookup",
            Description = "Look up name servers for a domain",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(NsLookupViewModel),
            ViewType = typeof(NsLookupPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Certificate Decoder",
            Description = "Decode and inspect X.509 certificates",
            Category = ToolCategory.Encryption,
            ViewModelType = typeof(CertificateDecoderViewModel),
            ViewType = typeof(CertificateDecoderPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "PEM Viewer",
            Description = "View and parse PEM certificates",
            Category = ToolCategory.Encryption,
            ViewModelType = typeof(PemViewerViewModel),
            ViewType = typeof(PemViewerPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Traceroute",
            Description = "Trace network route to a host",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(TracerouteViewModel),
            ViewType = typeof(TraceroutePage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Base64 Image Converter",
            Description = "Convert images to Base64 strings and vice versa",
            Category = ToolCategory.EncodersDecoders,
            ViewModelType = typeof(Base64ImageConverterViewModel),
            ViewType = typeof(Base64ImageConverterPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "RGB Color Converter",
            Description = "Convert colors between RGB, HEX, and HSL formats",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(RgbColorViewModel),
            ViewType = typeof(RgbColorPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Lorem Ipsum Generator",
            Description = "Generate dummy placeholder text for design and testing",
            Category = ToolCategory.TextAndString,
            ViewModelType = typeof(LoremIpsumViewModel),
            ViewType = typeof(LoremIpsumPage),
            Execute = async (input) => "Lorem ipsum dolor sit amet..."
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Word Counter Pro",
            Description = "Count words, characters, sentences and get detailed text statistics",
            Category = ToolCategory.TextAndString,
            ViewModelType = typeof(WordCounterViewModel),
            ViewType = typeof(WordCounterPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Unit Converter",
            Description = "Convert between length, weight, temperature, area, volume, speed, time, and data units",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(UnitConverterViewModel),
            ViewType = typeof(UnitConverterPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Markdown to HTML",
            Description = "Convert Markdown to HTML using Markdig",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(MarkdownViewModel),
            ViewType = typeof(MarkdownPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "REST API Client",
            Description = "Send HTTP requests (GET/POST/PUT/DELETE/PATCH/HEAD)",
            Category = ToolCategory.ApiWeb,
            ViewModelType = typeof(RestApiClientViewModel),
            ViewType = typeof(RestApiClientPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "User Agent Parser",
            Description = "Parse User-Agent strings to detect browser, OS and device",
            Category = ToolCategory.NetworkScanner,
            ViewModelType = typeof(UserAgentParserViewModel),
            ViewType = typeof(UserAgentParserPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "YAML to JSON",
            Description = "Convert YAML documents to JSON",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(YamlToJsonViewModel),
            ViewType = typeof(YamlToJsonPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "Pwned Password Checker",
            Description = "Check if a password appears in known data breaches (HIBP)",
            Category = ToolCategory.PasswordCredential,
            ViewModelType = typeof(PwnedViewModel),
            ViewType = typeof(PwnedPage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

        ToolRegistry.Register(new ToolDefinition
        {
            Name = "MIME Type Lookup",
            Description = "Look up MIME types by extension or vice versa",
            Category = ToolCategory.DeveloperTools,
            ViewModelType = typeof(MimeTypeViewModel),
            ViewType = typeof(MimeTypePage),
            Execute = async (input) => input?.ToString() ?? string.Empty
        });

    }

}