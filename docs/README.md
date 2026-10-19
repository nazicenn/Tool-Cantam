# Tool-Cantam

**A modular developer toolkit for Windows.**

Tool-Cantam is a Windows desktop application that brings developer utilities, text processing tools, network diagnostics, cryptographic operations, and API helpers into one place.

Built with **C#, WPF, and .NET 10**, the project focuses on modular tool registration, maintainable application architecture, and practical utilities for everyday development workflows.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/Language-C%23-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![WPF](https://img.shields.io/badge/UI-WPF-0078D4?logo=windows&logoColor=white)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Features

- **Developer productivity:** encoding, formatting, conversion, and text-processing utilities.
- **Network diagnostics:** DNS lookups, IP utilities, port scanning, HTTP headers, and SSL checks.
- **Cryptography:** hashing, HMAC, AES, RSA, JWT, and certificate-related tools.
- **API and web utilities:** HTTP request testing, REST API requests, cURL generation, and QR codes.
- **File and metadata utilities:** image metadata inspection and cleaning.
- **Modular registration:** centralized tool definitions with names, descriptions, categories, ViewModels, and views.
- **Desktop experience:** a native Windows application built with WPF.
- **Testability:** an xUnit test project for automated verification.

Tool availability and feature behavior depend on each tool's implementation and its external service requirements.

---

## Tool Catalog

The following catalog is based on the tool registrations in `ToolDefinitions.cs`. It describes the registered tool definitions, not a guarantee that every feature has identical implementation depth.

### Encoders & Decoders

| Tool | Purpose |
|---|---|
| Base64 Codec | Base64 conversion |
| URL Encoder/Decoder | URL encoding and decoding |
| Hex Encoder/Decoder | Hexadecimal conversion |
| HTML Encoder/Decoder | HTML entity conversion |
| Binary Encoder/Decoder | Binary representation conversion |
| ROT13 Transform | ROT13 text transformation |
| Character Code Converter | Character and code conversion |
| Unicode Converter | Unicode representation conversion |
| Base64 Image Converter | Base64 image conversion |

### Hash & Crypto

| Tool | Purpose |
|---|---|
| Hash Generator | Generate hashes |
| Hash Identifier | Identify possible hash formats |
| HMAC Generator | Generate keyed message authentication codes |
| AES Encrypt/Decrypt | AES encryption and decryption |
| RSA Key Generator | Generate RSA key pairs |
| Bcrypt Generator | Bcrypt password hashing |

### Network & Scanner

| Tool | Purpose |
|---|---|
| Port Scanner | Check port accessibility |
| Subdomain Scanner | Subdomain discovery |
| DNS Lookup | DNS record lookup |
| Reverse DNS | Reverse DNS lookup |
| IP Info | IP information lookup |
| WHOIS Lookup | Domain registration lookup |
| MAC Lookup | MAC address vendor lookup |
| SSL Checker | SSL/TLS certificate checks |
| Ping Test | Network reachability checks |
| HTTP Header Analyzer | Inspect HTTP headers |
| IP Calculator | IP and subnet calculations |
| GeoIP Lookup | IP geolocation lookup |
| MX Lookup | Mail exchange record lookup |
| NS Lookup | Name server lookup |
| Traceroute | Trace network routes |
| User Agent Parser | Parse user-agent strings |
| CORS Header Analyzer | Inspect CORS headers |

### Text & String

| Tool | Purpose |
|---|---|
| Case Converter | Change text casing |
| Text Counter | Count text characters and lines |
| String Reverser | Reverse text |
| Slug Generator | Generate URL-friendly slugs |
| Find and Replace | Find and replace text |
| Text Diff | Compare text |
| Text Sorter | Sort lines of text |
| Lorem Ipsum Generator | Generate placeholder text |
| Word Counter Pro | Analyze word counts |

### Developer Tools

| Tool | Purpose |
|---|---|
| JSON Formatter | Format JSON |
| UUID Generator | Generate UUIDs |
| Random String Generator | Generate random strings |
| Regex Tester | Test regular expressions |
| SQL Formatter | Format SQL |
| XML Formatter | Format XML |
| CSV to JSON | Convert CSV to JSON |
| YAML Formatter | Format YAML |
| JSON Minifier | Minify JSON |
| CSS Gradient Generator | Generate CSS gradients |
| MIME Type Lookup | Look up MIME types |
| Git Command Generator | Generate Git commands |
| Unit Converter | Convert measurement units |
| Markdown to HTML | Convert Markdown to HTML |

### File & Metadata

| Tool | Purpose |
|---|---|
| Metadata Cleaner | Clean supported image metadata |
| EXIF Viewer | Inspect EXIF metadata |

### Password & Credential

| Tool | Purpose |
|---|---|
| Password Generator | Generate passwords |
| Secret Generator | Generate secret values |
| Pwned Password Checker | Check password exposure |

### 📅 Date & Time

| Tool | Purpose |
|---|---|
| Unix Timestamp | Convert timestamps |
| Cron Parser | Parse cron expressions |

### API & Web

| Tool | Purpose |
|---|---|
| QR Code Generator | Generate QR codes |
| QR Code Reader | Read QR codes |
| HTTP Request Tester | Test HTTP requests |
| cURL Generator | Generate cURL commands |
| Fake Data Generator | Generate sample data |
| REST API Client | Send REST API requests |

### Encryption

| Tool | Purpose |
|---|---|
| JWT Decoder | Decode JWT payloads |
| JWT Generator | Generate JWTs |
| Certificate Decoder | Inspect certificates |
| PEM Viewer | View PEM-formatted data |

**Catalog summary:** 73 registered tool definitions across 10 categories, based on the supplied `ToolDefinitions.cs` source.

---

## Architecture

Tool-Cantam uses a centralized registration system to organize tools and associate their metadata with corresponding ViewModels and WPF views.

### Technology Stack

| Technology | Role |
|---|---|
| C# | Application development |
| .NET 10 | Runtime and framework |
| WPF | Windows desktop UI |
| CommunityToolkit.Mvvm | MVVM infrastructure |
| WPF UI | UI components |
| Serilog / Microsoft logging abstractions | Application logging |
| xUnit | Automated testing |

### Tool Structure

The intended structure for a fully implemented tool follows this pattern:

```text
Services/
├── Interfaces/
│   └── I[Tool]Service.cs
├── ErrorCodes/
│   └── [Tool]ErrorCodes.cs
└── [Tool]Service.cs

ViewModels/
└── [Tool]ViewModel.cs

Views/
├── [Tool]Page.xaml
└── [Tool]Page.xaml.cs

tests/tool-cantam.Tests/
├── [Tool]ServiceTests.cs
└── [Tool]ViewModelTests.cs

Resources/Languages/
├── en-US.xaml
└── tr-TR.xaml

Infrastructure/
└── ToolDefinitions.cs
```

This is the target structure for complete tool implementations. Individual tools may differ according to their current implementation.

### Design Principles

- **Separation of concerns:** separate presentation from reusable service logic where appropriate.
- **MVVM:** use ViewModels to manage UI state and commands.
- **Centralized registration:** keep tool metadata and registration in a consistent location.
- **Consistent error handling:** use shared result and error abstractions where implemented.
- **Asynchronous operations:** support cancellation for operations that may take significant time.
- **Structured logging:** capture useful diagnostic information without exposing sensitive content.
- **Localization:** keep user-facing strings in language resources.
- **Testability:** design services and ViewModels so their behavior can be tested independently.

These principles guide ongoing development; they should not be interpreted as a guarantee that every registered tool already implements every principle.

---

## Build & Test

### Prerequisites

- Windows 10 or Windows 11
- .NET 10 SDK
- Git

### Clone the Repository

```bash
git clone https://github.com/nazicenn/Tool-Cantam.git
cd Tool-Cantam
```

### Build

```bash
dotnet restore
dotnet build -m:1
```

### Run Tests

```bash
dotnet test
```

To run the WPF application, open the solution in Visual Studio and set the WPF application project as the startup project.

Test totals and results can change as the project evolves. Use the latest test output when reporting the number of passing tests.

---

## Localization

The project is designed to support:

- English (`en-US`)
- Turkish (`tr-TR`)

Localization guidelines:

- Use consistent resource keys, such as `L.[Tool].*`.
- Keep corresponding translations synchronized across language files.
- Avoid hard-coded user-facing text where localization is required.
- Verify missing resources and language-switching behavior.

New translations should be tested to ensure that resource keys and values are correctly resolved by the application.

---

## Privacy & Security

Tool-Cantam includes tools related to network diagnostics, credentials, cryptography, and data processing.

Security considerations:

- **External services:** network and lookup utilities may require internet access. Review individual tools to determine which services they contact.
- **Password exposure checks:** verify the implementation and data transmitted to the configured breach-checking service.
- **Cryptographic operations:** review algorithms, key derivation, IV handling, and key storage before using encryption features for sensitive data.
- **Logging:** avoid recording passwords, access tokens, cryptographic keys, or other sensitive payloads.
- **Network scanning:** only scan systems and networks for which you have authorization.
- **Offline behavior:** locally processed tools may work without internet access, while external lookup tools may not.

Tool-Cantam is a developer utility toolkit, not a replacement for a security audit or a dedicated secrets-management system.

---

## Roadmap

Potential areas for continued development include:

- Expanding automated test coverage
- Improving consistency between registered tools and their implementations
- Strengthening validation and error handling
- Improving accessibility and keyboard navigation
- Verifying localization consistency across all tools
- Reducing unnecessary coupling between UI and infrastructure
- Improving contributor documentation

The roadmap describes development opportunities rather than completed features.

---

## 🤝 Contributing

Contributions, bug reports, and suggestions are welcome.

1. Fork the repository.
2. Create a feature branch:

   ```bash
   git checkout -b feat/my-tool
   ```

3. Follow the existing architecture and naming conventions.
4. Add or update tests for changed behavior.
5. Update English and Turkish localization resources where applicable.
6. Build the solution and run the relevant tests.
7. Commit with a descriptive message.
8. Open a pull request describing the changes and verification performed.

### New Tool Checklist

- [ ] Define the service interface and required models.
- [ ] Implement service behavior and error handling.
- [ ] Add tool-specific error codes where appropriate.
- [ ] Implement the ViewModel and commands.
- [ ] Create the WPF view and consider accessibility.
- [ ] Add service and ViewModel tests.
- [ ] Update `en-US.xaml` and `tr-TR.xaml`.
- [ ] Register the tool in `ToolDefinitions.cs`.
- [ ] Run `dotnet build`.
- [ ] Run `dotnet test` and review the results.

---

## 📄 License

This project is distributed under the MIT License. See [LICENSE](LICENSE) for details.

---

**Tool-Cantam — practical developer utilities in a modular Windows desktop application.**
