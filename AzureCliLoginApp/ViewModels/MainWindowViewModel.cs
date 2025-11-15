using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using AzureCliLoginApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzureCliLoginApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "Ready to login to Azure CLI";

    [ObservableProperty]
    private string _outputLog = "Output will appear here...\n";

    [ObservableProperty]
    private bool _isLoginInProgress = false;

    [ObservableProperty]
    private string _statusColor = "#3498db";

    [ObservableProperty]
    private ObservableCollection<VirtualMachine> _virtualMachines = new();

    [ObservableProperty]
    private ObservableCollection<NetworkSecurityGroup> _networkSecurityGroups = new();

    [ObservableProperty]
    private ObservableCollection<InboundSecurityRule> _inboundSecurityRules = new();

    [ObservableProperty]
    private bool _hasResources = false;

    [ObservableProperty]
    private string _userIpAddress = "Not detected";

    [RelayCommand]
    private async Task LoginWithDeviceCode()
    {
        var success = await ExecuteAzCommand("az login --use-device-code", "Device Code Login");
        if (success)
        {
            await FetchResources();
        }
    }

    [RelayCommand]
    private async Task LoginInteractive()
    {
        var success = await ExecuteAzCommand("az login", "Interactive Login");
        if (success)
        {
            await FetchResources();
        }
    }

    [RelayCommand]
    private async Task CheckLoginStatus()
    {
        var success = await ExecuteAzCommand("az account show", "Account Status Check");
        if (success)
        {
            await FetchResources();
        }
    }

    [RelayCommand]
    private async Task Logout()
    {
        await ExecuteAzCommand("az logout", "Logout");
    }

    [RelayCommand]
    private void ClearOutput()
    {
        OutputLog = "";
        StatusMessage = "Output cleared";
        StatusColor = "#3498db";
    }

    [RelayCommand]
    private async Task RefreshResources()
    {
        await FetchResources();
    }

    private async Task FetchResources()
    {
        IsLoginInProgress = true;
        StatusMessage = "Fetching resources...";
        StatusColor = "#f39c12";
        AppendToLog("\n=== Fetching Resources ===\n");

        try
        {
            // Fetch user's IP address
            await FetchUserIpAddress();
            
            // Fetch VMs with permission check
            await FetchVirtualMachines();
            
            // Fetch NSGs with permission check
            await FetchNetworkSecurityGroups();

            // Fetch inbound security rules
            await FetchInboundSecurityRules();

            HasResources = VirtualMachines.Count > 0 || NetworkSecurityGroups.Count > 0;
            
            if (HasResources)
            {
                StatusMessage = $"Found {VirtualMachines.Count} VMs, {NetworkSecurityGroups.Count} NSGs, and {InboundSecurityRules.Count} inbound rules";
                StatusColor = "#27ae60";
                AppendToLog($"\nTotal: {VirtualMachines.Count} VMs, {NetworkSecurityGroups.Count} NSGs, {InboundSecurityRules.Count} inbound rules\n");
            }
            else
            {
                StatusMessage = "No resources found with edit permissions";
                StatusColor = "#f39c12";
                AppendToLog("\nNo resources found with the required permissions.\n");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error fetching resources: {ex.Message}";
            StatusColor = "#e74c3c";
            AppendToLog($"\n[EXCEPTION] {ex.Message}\n");
        }
        finally
        {
            IsLoginInProgress = false;
        }
    }

    private async Task FetchVirtualMachines()
    {
        AppendToLog("Fetching Virtual Machines...\n");
        VirtualMachines.Clear();

        try
        {
            // Get all VMs in JSON format that the user has access to
            // Azure CLI naturally filters results based on user permissions
            var output = await ExecuteAzCommandWithOutput(
                "az vm list --query \"[].{name:name, resourceGroup:resourceGroup, location:location, id:id}\" -o json"
            );

            if (!string.IsNullOrWhiteSpace(output))
            {
                var vms = JsonSerializer.Deserialize<VirtualMachine[]>(output);
                if (vms != null)
                {
                    foreach (var vm in vms)
                    {
                        VirtualMachines.Add(vm);
                        AppendToLog($"  ✓ {vm.Name} (RG: {vm.ResourceGroup})\n");
                    }
                }
            }

            AppendToLog($"Found {VirtualMachines.Count} VMs accessible to the user.\n");
        }
        catch (Exception ex)
        {
            AppendToLog($"[ERROR] Failed to fetch VMs: {ex.Message}\n");
        }
    }

    private async Task FetchNetworkSecurityGroups()
    {
        AppendToLog("Fetching Network Security Groups...\n");
        NetworkSecurityGroups.Clear();

        try
        {
            // Get all NSGs in JSON format that the user has access to
            // Azure CLI naturally filters results based on user permissions
            var output = await ExecuteAzCommandWithOutput(
                "az network nsg list --query \"[].{name:name, resourceGroup:resourceGroup, location:location, id:id}\" -o json"
            );

            if (!string.IsNullOrWhiteSpace(output))
            {
                var nsgs = JsonSerializer.Deserialize<NetworkSecurityGroup[]>(output);
                if (nsgs != null)
                {
                    foreach (var nsg in nsgs)
                    {
                        NetworkSecurityGroups.Add(nsg);
                        AppendToLog($"  ✓ {nsg.Name} (RG: {nsg.ResourceGroup})\n");
                    }
                }
            }

            AppendToLog($"Found {NetworkSecurityGroups.Count} NSGs accessible to the user.\n");
        }
        catch (Exception ex)
        {
            AppendToLog($"[ERROR] Failed to fetch NSGs: {ex.Message}\n");
        }
    }

    private async Task<string> ExecuteAzCommandWithOutput(string command)
    {
        var output = string.Empty;
        
        var processInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // For Windows, use cmd.exe instead
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            processInfo.FileName = "cmd.exe";
            processInfo.Arguments = $"/c {command}";
        }

        using var process = new Process { StartInfo = processInfo };
        process.Start();
        
        output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }

    private async Task<bool> ExecuteAzCommand(string command, string operationName)
    {
        IsLoginInProgress = true;
        StatusMessage = $"{operationName} in progress...";
        StatusColor = "#f39c12";
        AppendToLog($"\n=== {operationName} Started ===\n");
        AppendToLog($"Command: {command}\n\n");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // For Windows, use cmd.exe instead
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                processInfo.FileName = "cmd.exe";
                processInfo.Arguments = $"/c {command}";
            }

            using var process = new Process { StartInfo = processInfo };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AppendToLog(e.Data + "\n");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AppendToLog($"[ERROR] {e.Data}\n");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var success = process.ExitCode == 0;
            
            if (success)
            {
                StatusMessage = $"{operationName} completed successfully";
                StatusColor = "#27ae60";
                AppendToLog($"\n=== {operationName} Completed Successfully ===\n");
            }
            else
            {
                StatusMessage = $"{operationName} failed with exit code {process.ExitCode}";
                StatusColor = "#e74c3c";
                AppendToLog($"\n=== {operationName} Failed (Exit Code: {process.ExitCode}) ===\n");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            StatusColor = "#e74c3c";
            AppendToLog($"\n[EXCEPTION] {ex.Message}\n");
            AppendToLog($"Make sure Azure CLI (az) is installed on your system.\n");
            return false;
        }
        finally
        {
            IsLoginInProgress = false;
        }
    }

    private async Task FetchUserIpAddress()
    {
        AppendToLog("Fetching user's public IPv4 address...\n");
        
        try
        {
            // Try multiple methods to get the public IP address
            string? ipAddress = null;
            
            // Method 1: Try using hostname -I (works on Linux)
            var hostOutput = await ExecuteAzCommandWithOutput("hostname -I 2>/dev/null");
            if (!string.IsNullOrWhiteSpace(hostOutput))
            {
                var ips = hostOutput.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    ipAddress = ips[0]; // First IP is usually the primary one
                }
            }
            
            // Method 2: Try using ip command (works on most Linux systems)
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                var ipOutput = await ExecuteAzCommandWithOutput("ip addr show | grep 'inet ' | grep -v '127.0.0.1' | head -1 | awk '{print $2}' | cut -d/ -f1");
                if (!string.IsNullOrWhiteSpace(ipOutput))
                {
                    ipAddress = ipOutput.Trim();
                }
            }
            
            // Method 3: Try using ipconfig on Windows
            if (string.IsNullOrWhiteSpace(ipAddress) && Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var ipconfigOutput = await ExecuteAzCommandWithOutput("ipconfig | findstr /i \"IPv4\"");
                if (!string.IsNullOrWhiteSpace(ipconfigOutput))
                {
                    var parts = ipconfigOutput.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        ipAddress = parts[1].Trim();
                    }
                }
            }
            
            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                UserIpAddress = ipAddress;
                AppendToLog($"  ✓ User IP Address: {ipAddress}\n");
            }
            else
            {
                UserIpAddress = "Not detected";
                AppendToLog("  ⚠ Could not detect IP address\n");
            }
        }
        catch (Exception ex)
        {
            AppendToLog($"[ERROR] Failed to fetch IP address: {ex.Message}\n");
            UserIpAddress = "Error";
        }
    }

    private async Task FetchInboundSecurityRules()
    {
        AppendToLog("Fetching Inbound Security Rules...\n");
        InboundSecurityRules.Clear();

        try
        {
            // For each NSG, fetch its security rules
            foreach (var nsg in NetworkSecurityGroups)
            {
                var output = await ExecuteAzCommandWithOutput(
                    $"az network nsg rule list --nsg-name \"{nsg.Name}\" --resource-group \"{nsg.ResourceGroup}\" " +
                    "--query \"[?direction=='Inbound'].{name:name, priority:priority, sourceAddressPrefix:sourceAddressPrefix, " +
                    "sourcePortRange:sourcePortRange, destinationAddressPrefix:destinationAddressPrefix, " +
                    "destinationPortRange:destinationPortRange, protocol:protocol, access:access, direction:direction}\" -o json"
                );

                if (!string.IsNullOrWhiteSpace(output))
                {
                    var rules = JsonSerializer.Deserialize<InboundSecurityRule[]>(output);
                    if (rules != null)
                    {
                        foreach (var rule in rules)
                        {
                            rule.NsgName = nsg.Name;
                            rule.ResourceGroup = nsg.ResourceGroup;
                            InboundSecurityRules.Add(rule);
                            AppendToLog($"  ✓ {rule.Name} (NSG: {nsg.Name}, Port: {rule.DestinationPortRange}, Access: {rule.Access})\n");
                        }
                    }
                }
            }

            AppendToLog($"Found {InboundSecurityRules.Count} inbound security rules.\n");
        }
        catch (Exception ex)
        {
            AppendToLog($"[ERROR] Failed to fetch inbound security rules: {ex.Message}\n");
        }
    }

    private void AppendToLog(string message)
    {
        OutputLog += message;
    }
}
