// ViewModels/FakeDataGeneratorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.Json;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class FakeDataGeneratorViewModel : ObservableObject
{
    [ObservableProperty]
    private int _count = 5;

    [ObservableProperty]
    private bool _includeName = true;

    [ObservableProperty]
    private bool _includeEmail = true;

    [ObservableProperty]
    private bool _includePhone = true;

    [ObservableProperty]
    private bool _includeAddress = true;

    [ObservableProperty]
    private bool _includeCompany = true;

    [ObservableProperty]
    private string _output = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    private readonly Random _random = new();

    private readonly string[] _firstNames = { "Ahmet", "Mehmet", "Ali", "Ayşe", "Fatma", "Zeynep", "Mustafa", "Can", "Elif", "Emre" };
    private readonly string[] _lastNames = { "Yılmaz", "Demir", "Kaya", "Çelik", "Şahin", "Öztürk", "Arslan", "Polat" };
    private readonly string[] _companies = { "TechCorp", "DataSoft", "CyberSec", "DevStudio", "CloudNet", "AI Labs", "SoftWare", "NetSolve" };
    private readonly string[] _cities = { "İstanbul", "Ankara", "İzmir", "Bursa", "Antalya", "Kocaeli", "Gaziantep" };
    private readonly string[] _streets = { "Atatürk Cad.", "İstiklal Cad.", "Cumhuriyet Cad.", "Kurtuluş Sok.", "Barış Sok.", "Özgürlük Cad." };

    [RelayCommand]
    private void Generate()
    {
        try
        {
            var data = new System.Text.StringBuilder();

            if (Count == 1)
            {
                data.AppendLine("{");
                AddField(data, "name", GetFullName(), IncludeName);
                AddField(data, "email", GetEmail(), IncludeEmail);
                AddField(data, "phone", GetPhone(), IncludePhone);
                AddField(data, "address", GetAddress(), IncludeAddress);
                AddField(data, "company", GetCompany(), IncludeCompany);
                data.AppendLine("}");
            }
            else
            {
                data.AppendLine("[");
                for (int i = 0; i < Count; i++)
                {
                    data.AppendLine("  {");
                    AddField(data, "name", GetFullName(), IncludeName, "  ");
                    AddField(data, "email", GetEmail(), IncludeEmail, "  ");
                    AddField(data, "phone", GetPhone(), IncludePhone, "  ");
                    AddField(data, "address", GetAddress(), IncludeAddress, "  ");
                    AddField(data, "company", GetCompany(), IncludeCompany, "  ");
                    data.AppendLine("  }" + (i < Count - 1 ? "," : ""));
                }
                data.AppendLine("]");
            }

            Output = data.ToString();
            StatusText = $"✓ Generated {Count} fake record(s)";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    private void AddField(System.Text.StringBuilder sb, string key, string value, bool include, string indent = "")
    {
        if (include)
        {
            sb.AppendLine($"{indent}  \"{key}\": \"{value}\",");
        }
    }

    private string GetFullName() => $"{_firstNames[_random.Next(_firstNames.Length)]} {_lastNames[_random.Next(_lastNames.Length)]}";
    private string GetEmail() => $"{GetFullName().ToLower().Replace(" ", ".")}@example.com";
    private string GetPhone() => $"+90 5{_random.Next(30, 60)} {_random.Next(100, 999)} {_random.Next(10, 99)} {_random.Next(10, 99)}";
    private string GetAddress() => $"{_streets[_random.Next(_streets.Length)]} No: {_random.Next(1, 100)}, {_cities[_random.Next(_cities.Length)]}";
    private string GetCompany() => _companies[_random.Next(_companies.Length)];

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(Output))
        {
            Clipboard.SetText(Output);
            StatusText = "Copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Output = string.Empty;
        StatusText = "Cleared";
    }
}