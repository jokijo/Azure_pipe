using System;
using System.Diagnostics;
using System.Threading.Tasks;
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

    [RelayCommand]
    private async Task LoginWithDeviceCode()
    {
        await ExecuteAzCommand("az login --use-device-code", "Device Code Login");
    }

    [RelayCommand]
    private async Task LoginInteractive()
    {
        await ExecuteAzCommand("az login", "Interactive Login");
    }

    [RelayCommand]
    private async Task CheckLoginStatus()
    {
        await ExecuteAzCommand("az account show", "Account Status Check");
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

    private async Task ExecuteAzCommand(string command, string operationName)
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

            if (process.ExitCode == 0)
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            StatusColor = "#e74c3c";
            AppendToLog($"\n[EXCEPTION] {ex.Message}\n");
            AppendToLog($"Make sure Azure CLI (az) is installed on your system.\n");
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
