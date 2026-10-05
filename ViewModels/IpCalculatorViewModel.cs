// ViewModels/IpCalculatorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Net;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class IpCalculatorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _ipAddress = "192.168.1.0";

    [ObservableProperty]
    private int _cidr = 24;

    [ObservableProperty]
    private string _networkAddress = string.Empty;

    [ObservableProperty]
    private string _broadcastAddress = string.Empty;

    [ObservableProperty]
    private string _firstUsable = string.Empty;

    [ObservableProperty]
    private string _lastUsable = string.Empty;

    [ObservableProperty]
    private string _subnetMask = string.Empty;

    [ObservableProperty]
    private string _wildcardMask = string.Empty;

    [ObservableProperty]
    private int _totalHosts = 0;

    [ObservableProperty]
    private int _usableHosts = 0;

    [ObservableProperty]
    private string _binaryIp = string.Empty;

    [ObservableProperty]
    private string _binaryMask = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Calculate()
    {
        if (string.IsNullOrWhiteSpace(IpAddress))
        {
            StatusText = "Please enter an IP address";
            return;
        }

        try
        {
            if (!IPAddress.TryParse(IpAddress, out var ip))
            {
                StatusText = "Invalid IP address format";
                return;
            }

            if (Cidr < 0 || Cidr > 32)
            {
                StatusText = "CIDR must be between 0 and 32";
                return;
            }

            var ipBytes = ip.GetAddressBytes();

            // Binary IP
            BinaryIp = string.Join(".", Array.ConvertAll(ipBytes, b => Convert.ToString(b, 2).PadLeft(8, '0')));

            // Subnet Mask
            uint mask = 0xFFFFFFFF << (32 - Cidr);
            var maskBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)mask));
            SubnetMask = new IPAddress(maskBytes).ToString();
            BinaryMask = string.Join(".", Array.ConvertAll(maskBytes, b => Convert.ToString(b, 2).PadLeft(8, '0')));

            // Wildcard
            var wildcardBytes = Array.ConvertAll(maskBytes, b => (byte)~b);
            WildcardMask = new IPAddress(wildcardBytes).ToString();

            // Network
            var networkBytes = new byte[4];
            for (int i = 0; i < 4; i++)
                networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            NetworkAddress = new IPAddress(networkBytes).ToString();

            // Broadcast
            var broadcastBytes = new byte[4];
            for (int i = 0; i < 4; i++)
                broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
            BroadcastAddress = new IPAddress(broadcastBytes).ToString();

            // Hosts
            TotalHosts = (int)Math.Pow(2, 32 - Cidr);
            UsableHosts = Math.Max(0, TotalHosts - 2);

            // First Usable
            var firstBytes = (byte[])networkBytes.Clone();
            if (UsableHosts > 0)
            {
                for (int i = 3; i >= 0; i--)
                {
                    if (firstBytes[i] < 255)
                    {
                        firstBytes[i]++;
                        break;
                    }
                }
            }
            FirstUsable = new IPAddress(firstBytes).ToString();

            // Last Usable
            var lastBytes = (byte[])broadcastBytes.Clone();
            if (UsableHosts > 0)
            {
                for (int i = 3; i >= 0; i--)
                {
                    if (lastBytes[i] > 0)
                    {
                        lastBytes[i]--;
                        break;
                    }
                }
            }
            LastUsable = new IPAddress(lastBytes).ToString();

            StatusText = $"✓ Calculated for {NetworkAddress}/{Cidr}";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void LoadSample()
    {
        IpAddress = "192.168.1.0";
        Cidr = 24;
        Calculate();
    }

    [RelayCommand]
    private void Clear()
    {
        NetworkAddress = string.Empty;
        BroadcastAddress = string.Empty;
        FirstUsable = string.Empty;
        LastUsable = string.Empty;
        SubnetMask = string.Empty;
        WildcardMask = string.Empty;
        TotalHosts = 0;
        UsableHosts = 0;
        BinaryIp = string.Empty;
        BinaryMask = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyResult()
    {
        var result = $"Network: {NetworkAddress}/{Cidr}\n" +
                     $"Subnet Mask: {SubnetMask}\n" +
                     $"Wildcard: {WildcardMask}\n" +
                     $"Broadcast: {BroadcastAddress}\n" +
                     $"First Usable: {FirstUsable}\n" +
                     $"Last Usable: {LastUsable}\n" +
                     $"Total Hosts: {TotalHosts}\n" +
                     $"Usable Hosts: {UsableHosts}";
        Clipboard.SetText(result);
        StatusText = "Copied to clipboard!";
    }
}