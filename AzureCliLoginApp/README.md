# Azure CLI Login App

A cross-platform desktop application built with Avalonia UI and C# that provides a graphical interface for logging into Azure CLI.

## Features

- **Multiple Login Methods**:
  - Device Code Login - Login using a device code (useful for headless environments)
  - Interactive Login - Standard browser-based authentication
  - Check Status - Verify current Azure CLI login status

- **Resource Discovery**: Automatically fetch and display Azure resources after login
  - Virtual Machines (VMs) - Shows all VMs the user has access to
  - Network Security Groups (NSGs) - Shows all NSGs the user has access to
  - Resources are automatically filtered based on user's Azure RBAC permissions
  - Manual refresh option available

- **Real-time Output**: View command output in real-time as the Azure CLI executes commands

- **Status Indicators**: Color-coded status messages (blue for info, orange for in-progress, green for success, red for errors)

- **Split-Panel Interface**: 
  - Left panel displays command output and execution logs
  - Right panel displays discovered Azure resources (VMs and NSGs)

- **Output Management**: Clear output log and logout functionality

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) or later
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed and available in your system PATH

## Building and Running

### Build the Application

```bash
cd AzureCliLoginApp
dotnet build
```

### Run the Application

```bash
cd AzureCliLoginApp
dotnet run
```

### Publish for Distribution

For Windows:
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

For Linux:
```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

For macOS:
```bash
dotnet publish -c Release -r osx-x64 --self-contained
```

## Usage

1. Launch the application
2. Select one of the login methods:
   - **Device Code Login**: Follow the instructions in the output to authenticate using a device code
   - **Interactive Login**: Opens your default browser for authentication
   - **Check Status**: Displays current Azure account information
3. After successful login, the application automatically fetches and displays:
   - Virtual Machines (VMs) you have access to
   - Network Security Groups (NSGs) you have access to
4. View the command output in the left panel
5. View your accessible Azure resources in the right panel
6. Use "Refresh Resources" to manually update the resource list
7. Use "Clear Output" to clear the command log
8. Use "Logout" to sign out from Azure CLI

## Architecture

The application follows the MVVM (Model-View-ViewModel) pattern using:
- **Avalonia UI**: Cross-platform UI framework
- **CommunityToolkit.Mvvm**: MVVM implementation with source generators
- **Process**: Executes Azure CLI commands and captures output

## Project Structure

```
AzureCliLoginApp/
├── Views/
│   └── MainWindow.axaml       # UI layout
├── ViewModels/
│   ├── MainWindowViewModel.cs # Business logic and commands
│   └── ViewModelBase.cs       # Base class for view models
├── Assets/
│   └── avalonia-logo.ico      # Application icon
├── Program.cs                 # Application entry point
└── App.axaml.cs              # Application initialization
```

## Technologies

- **Avalonia UI 11.3.8**: Cross-platform XAML-based UI framework
- **.NET 9.0**: Modern .NET platform
- **CommunityToolkit.Mvvm 8.2.1**: MVVM helpers and source generators

## License

This project is open source and available under the MIT License.
