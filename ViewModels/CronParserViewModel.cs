// ViewModels/CronParserViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class CronParserViewModel : ObservableObject
{
    [ObservableProperty]
    private string _cronExpression = "0 0 * * *";

    [ObservableProperty]
    private string _result = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Parse()
    {
        if (string.IsNullOrWhiteSpace(CronExpression))
        {
            StatusText = "Please enter a cron expression";
            Result = string.Empty;
            return;
        }

        try
        {
            var parts = CronExpression.Trim().Split(' ');
            if (parts.Length != 5)
            {
                Result = "Invalid: Need 5 parts (minute hour day month weekday)";
                StatusText = "Invalid cron expression";
                return;
            }

            var minute = parts[0];
            var hour = parts[1];
            var day = parts[2];
            var month = parts[3];
            var weekday = parts[4];

            var sb = new StringBuilder();
            sb.AppendLine("📅 Cron Expression Analysis:");
            sb.AppendLine();

            sb.AppendLine($"🕐 Minute: {FormatField(minute, "0-59")}");
            sb.AppendLine($"🕐 Hour: {FormatField(hour, "0-23")}");
            sb.AppendLine($"📆 Day of Month: {FormatField(day, "1-31")}");
            sb.AppendLine($"📅 Month: {FormatMonth(month)}");
            sb.AppendLine($"📆 Day of Week: {FormatWeekday(weekday)}");

            sb.AppendLine();
            sb.AppendLine($"📝 Summary: {ToHumanReadable(parts)}");

            Result = sb.ToString();
            StatusText = "✓ Cron expression parsed";
        }
        catch (Exception ex)
        {
            Result = $"Error: {ex.Message}";
            StatusText = $"Error: {ex.Message}";
        }
    }

    private string FormatField(string field, string range)
    {
        if (field == "*") return $"Every {GetUnitName(range)} (all values)";
        if (field.Contains(",")) return $"At {field.Replace(",", ", ")}";
        if (field.Contains("-")) return $"From {field.Replace("-", " to ")}";
        if (field.Contains("/"))
        {
            var parts = field.Split('/');
            return $"Every {parts[1]} {GetUnitName(range)} starting from {parts[0]}";
        }
        return $"At {field}";
    }

    private string GetUnitName(string range)
    {
        return range switch
        {
            "0-59" => "minute",
            "0-23" => "hour",
            "1-31" => "day",
            _ => "unit"
        };
    }

    private string FormatMonth(string month)
    {
        if (month == "*") return "Every month";
        if (month.Contains(",")) return string.Join(", ", month.Split(',').Select(GetMonthName));
        if (month.Contains("-"))
        {
            var parts = month.Split('-');
            return $"{GetMonthName(parts[0])} to {GetMonthName(parts[1])}";
        }
        return GetMonthName(month);
    }

    private string FormatWeekday(string weekday)
    {
        if (weekday == "*") return "Every day of week";
        if (weekday == "0" || weekday == "7") return "Sunday";
        if (weekday.Contains(",")) return string.Join(", ", weekday.Split(',').Select(GetWeekdayName));
        if (weekday.Contains("-"))
        {
            var parts = weekday.Split('-');
            return $"{GetWeekdayName(parts[0])} to {GetWeekdayName(parts[1])}";
        }
        return GetWeekdayName(weekday);
    }

    private string GetMonthName(string month)
    {
        return month switch
        {
            "1" => "January",
            "2" => "February",
            "3" => "March",
            "4" => "April",
            "5" => "May",
            "6" => "June",
            "7" => "July",
            "8" => "August",
            "9" => "September",
            "10" => "October",
            "11" => "November",
            "12" => "December",
            _ => month
        };
    }

    private string GetWeekdayName(string day)
    {
        return day switch
        {
            "0" or "7" => "Sunday",
            "1" => "Monday",
            "2" => "Tuesday",
            "3" => "Wednesday",
            "4" => "Thursday",
            "5" => "Friday",
            "6" => "Saturday",
            _ => day
        };
    }

    private string ToHumanReadable(string[] parts)
    {
        var minute = parts[0];
        var hour = parts[1];
        var day = parts[2];
        var month = parts[3];
        var weekday = parts[4];

        if (minute == "0" && hour == "0" && day == "*" && month == "*" && weekday == "*")
            return "Every day at midnight (00:00)";

        if (minute == "0" && hour == "12" && day == "*" && month == "*" && weekday == "*")
            return "Every day at noon (12:00)";

        if (minute == "*/15" && hour == "*" && day == "*" && month == "*" && weekday == "*")
            return "Every 15 minutes";

        if (minute == "0" && hour == "9" && day == "*" && month == "*" && weekday == "1-5")
            return "Every weekday at 09:00";

        if (minute == "0" && hour == "0" && day == "1" && month == "*" && weekday == "*")
            return "1st day of every month at midnight";

        return $"{hour}:{minute} on {GetReadableDay(day)} of {GetReadableMonth(month)} on {GetReadableWeekday(weekday)}";
    }

    private string GetReadableDay(string day)
    {
        if (day == "*") return "every day";
        if (day.Contains("/")) return $"every {day.Split('/')[1]} days";
        if (day.Contains("-")) return $"day {day.Split('-')[0]} to {day.Split('-')[1]}";
        return $"day {day}";
    }

    private string GetReadableMonth(string month)
    {
        if (month == "*") return "every month";
        if (month.Contains("/")) return $"every {month.Split('/')[1]} months";
        return month;
    }

    private string GetReadableWeekday(string weekday)
    {
        if (weekday == "*") return "every day of week";
        if (weekday == "1-5") return "weekdays (Monday to Friday)";
        return $"day {weekday} of week";
    }

    [RelayCommand]
    private void LoadSampleDaily()
    {
        CronExpression = "0 0 * * *";
        Parse();
    }

    [RelayCommand]
    private void LoadSampleHourly()
    {
        CronExpression = "0 * * * *";
        Parse();
    }

    [RelayCommand]
    private void LoadSampleWeekly()
    {
        CronExpression = "0 9 * * 1";
        Parse();
    }

    [RelayCommand]
    private void LoadSampleMonthly()
    {
        CronExpression = "0 0 1 * *";
        Parse();
    }

    [RelayCommand]
    private void Clear()
    {
        CronExpression = string.Empty;
        Result = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (!string.IsNullOrWhiteSpace(Result))
        {
            Clipboard.SetText(Result);
            StatusText = "Copied to clipboard!";
        }
    }
}