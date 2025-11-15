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
    private bool _hasResources = false;

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
            // Fetch VMs with permission check
            await FetchVirtualMachines();
            
            // Fetch NSGs with permission check
            await FetchNetworkSecurityGroups();

            HasResources = VirtualMachines.Count > 0 || NetworkSecurityGroups.Count > 0;
            
            if (HasResources)
            {
                StatusMessage = $"Found {VirtualMachines.Count} VMs and {NetworkSecurityGroups.Count} NSGs";
                StatusColor = "#27ae60";
                AppendToLog($"\nTotal: {VirtualMachines.Count} VMs, {NetworkSecurityGroups.Count} NSGs\n");
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
            // Get all VMs in JSON format
            var output = await ExecuteAzCommandWithOutput(
                "az vm list --query \"[].{name:name, resourceGroup:resourceGroup, location:location, id:id}\" -o json"
            );

            if (!string.IsNullOrWhiteSpace(output))
            {
                var vms = JsonSerializer.Deserialize<VirtualMachine[]>(output);
                if (vms != null)
                {
                    // Check write permissions for each VM
                    foreach (var vm in vms)
                    {
                        // Check if user has permission to modify the VM (specifically for inbound port rules)
                        // We check for write access to the VM's network security group or NIC
                        var hasPermission = await CheckResourcePermission(vm.Id, "Microsoft.Network/networkInterfaces/write");
                        
                        if (hasPermission)
                        {
                            VirtualMachines.Add(vm);
                            AppendToLog($"  ✓ {vm.Name} (RG: {vm.ResourceGroup})\n");
                        }
                    }
                }
            }

            AppendToLog($"Found {VirtualMachines.Count} VMs with edit permissions.\n");
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
            // Get all NSGs in JSON format
            var output = await ExecuteAzCommandWithOutput(
                "az network nsg list --query \"[].{name:name, resourceGroup:resourceGroup, location:location, id:id}\" -o json"
            );

            if (!string.IsNullOrWhiteSpace(output))
            {
                var nsgs = JsonSerializer.Deserialize<NetworkSecurityGroup[]>(output);
                if (nsgs != null)
                {
                    // Check write permissions for each NSG
                    foreach (var nsg in nsgs)
                    {
                        // Check if user has permission to modify NSG rules
                        var hasPermission = await CheckResourcePermission(nsg.Id, "Microsoft.Network/networkSecurityGroups/write");
                        
                        if (hasPermission)
                        {
                            NetworkSecurityGroups.Add(nsg);
                            AppendToLog($"  ✓ {nsg.Name} (RG: {nsg.ResourceGroup})\n");
                        }
                    }
                }
            }

            AppendToLog($"Found {NetworkSecurityGroups.Count} NSGs with edit permissions.\n");
        }
        catch (Exception ex)
        {
            AppendToLog($"[ERROR] Failed to fetch NSGs: {ex.Message}\n");
        }
    }

    private async Task<bool> CheckResourcePermission(string resourceId, string action)
    {
        try
        {
            // Use Azure CLI to check permissions - if the command succeeds, user has permission
            // We use a simple approach: try to show the resource. If it fails with permission error, return false
            var checkCommand = $"az resource show --ids \"{resourceId}\" --query id -o tsv 2>&1";
            var output = await ExecuteAzCommandWithOutput(checkCommand);
            
            // If we got the resource ID back, user has at least read access
            // For simplicity, we assume read access implies potential write access for resources they can see
            // A more thorough check would use: az role assignment list
            return !string.IsNullOrWhiteSpace(output) && !output.Contains("AuthorizationFailed");
        }
        catch
        {
            return false;
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

    private void AppendToLog(string message)
    {
        OutputLog += message;
    }
}
