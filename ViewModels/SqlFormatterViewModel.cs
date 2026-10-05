// ViewModels/SqlFormatterViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class SqlFormatterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputSql = string.Empty;

    [ObservableProperty]
    private string _outputSql = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Format()
    {
        if (string.IsNullOrWhiteSpace(InputSql))
        {
            StatusText = "Please enter SQL to format";
            OutputSql = string.Empty;
            return;
        }

        try
        {
            var sql = InputSql;

            // Fazla boşlukları temizle
            sql = Regex.Replace(sql, @"\s+", " ");

            // Anahtar kelimelerden önce satır sonu ekle
            string[] keywords = {
                "SELECT", "FROM", "WHERE", "AND", "OR", "ORDER BY", "GROUP BY",
                "HAVING", "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN",
                "UNION", "INSERT", "UPDATE", "DELETE", "SET", "VALUES", "INTO"
            };

            foreach (var keyword in keywords)
            {
                sql = Regex.Replace(sql, $@"\b{keyword}\b", $"\n{keyword}", RegexOptions.IgnoreCase);
            }

            // Virgüllerden sonra yeni satır
            sql = Regex.Replace(sql, @",\s*", ",\n  ");

            // Operatörler etrafında boşluk
            sql = Regex.Replace(sql, @"\s*=\s*", " = ");
            sql = Regex.Replace(sql, @"\s*>\s*", " > ");
            sql = Regex.Replace(sql, @"\s*<\s*", " < ");

            // Fazla satır boşluklarını temizle
            sql = Regex.Replace(sql, @"\n\s*\n", "\n");
            sql = sql.Trim();

            OutputSql = sql;
            StatusText = "✓ SQL formatted successfully";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
            OutputSql = string.Empty;
        }
    }

    [RelayCommand]
    private void Minify()
    {
        if (string.IsNullOrWhiteSpace(InputSql))
        {
            StatusText = "Please enter SQL to minify";
            return;
        }

        try
        {
            var sql = InputSql;
            sql = Regex.Replace(sql, @"\s+", " ");
            sql = sql.Trim();
            OutputSql = sql;
            StatusText = "✓ SQL minified successfully";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
            OutputSql = string.Empty;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputSql = string.Empty;
        OutputSql = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(OutputSql))
        {
            Clipboard.SetText(OutputSql);
            StatusText = "Copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Swap()
    {
        (InputSql, OutputSql) = (OutputSql, InputSql);
        StatusText = "Swapped";
    }

    [RelayCommand]
    private void LoadSample()
    {
        InputSql = "SELECT id,name,email,city FROM users WHERE age>18 AND status='active' ORDER BY name";
        Format();
    }
}