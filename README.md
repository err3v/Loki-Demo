# Loki Demo - .NET Logging Application

A comprehensive .NET demo application that demonstrates logging integration with Grafana Cloud using Grafana Alloy as a local collector. This project showcases how to set up structured logging from .NET applications to Grafana Cloud via Alloy.

## 📋 Table of Contents

- [🚀 Features](#-features)
- [📖 What is Grafana Alloy?](#-what-is-grafana-alloy)
- [📋 Prerequisites](#-prerequisites)
- [🛠️ Setup Instructions](#️-setup-instructions)
  - [Step 1: Create Grafana Cloud Account](#step-1-create-grafana-cloud-account)
  - [Step 2: Download the Repository](#step-2-download-the-repository)
  - [Step 3: Configure Grafana Cloud Data Source](#step-3-configure-grafana-cloud-data-source)
  - [Step 4: Install Grafana Alloy](#step-4-install-grafana-alloy)
  - [Step 5: Update Configuration](#step-5-update-configuration)
  - [Step 6: Build and Run](#step-6-build-and-run)
  - [Step 7: Test the Connection](#step-7-test-the-connection)
  - [Step 8: Verify Logs in Grafana](#step-8-verify-logs-in-grafana)
- [📊 Viewing Logs](#-viewing-logs)
  - [Grafana Cloud](#grafana-cloud)
  - [File Logging](#file-logging)
  - [Windows Event Viewer](#windows-event-viewer)
- [🔧 Troubleshooting](#-troubleshooting)
  - [Common Issues](#common-issues)
  - [Debug Mode](#debug-mode)
- [📁 Project Structure](#-project-structure)
- [🧪 Testing Connections](#-testing-connections)
- [📚 Learning Resources](#-learning-resources)
- [🤝 Contributing](#-contributing)
- [📄 License](#-license)
- [🆘 Support](#-support)

## 🚀 Features

- **Grafana Cloud Integration**: Direct logging to Grafana Cloud via Alloy collector
- **Realistic Business Data**: Generates realistic business operation logs
- **Structured Logging**: Uses Serilog with structured data for better querying
- **Local Alloy Collector**: Uses Grafana Alloy to collect and forward logs
- **Configurable**: Easy configuration via JSON file
- **Cross-Platform**: Works on Windows, Linux, and macOS

## 📖 What is Grafana Alloy?

Grafana Alloy is an open-source OpenTelemetry collector that acts as a local agent for collecting, processing, and forwarding telemetry data (logs, metrics, traces) to Grafana Cloud. Think of it as a "data pipeline" that sits between your application and Grafana Cloud.

### How Alloy Works in This Setup:

1. **Local Collection**: Your .NET application sends logs to Alloy running on `localhost:1337`
2. **Data Processing**: Alloy receives the logs and can add labels, filter, or transform them
3. **Forwarding**: Alloy then forwards the processed logs to Grafana Cloud using your credentials
4. **Monitoring**: You can monitor Alloy's status at `http://localhost:12345` (web interface)

### Benefits of Using Alloy:

- **Reliable Delivery**: Alloy handles authentication and retries automatically
- **Local Buffering**: If Grafana Cloud is temporarily unavailable, Alloy can buffer logs locally
- **Data Enrichment**: Can add host information, labels, and other metadata
- **Flexible Configuration**: Supports multiple input sources and output destinations
- **Monitoring**: Built-in web interface for monitoring collector health and performance

## 📋 Prerequisites

- .NET 9.0 SDK or later
- Grafana Cloud account
- Windows (for Event Log functionality)

## 🛠️ Setup Instructions

### Step 1: Create Grafana Cloud Account
1. Go to [Grafana Cloud](https://grafana.com/auth/sign-up/create-user)
2. Create a free account (includes 3GB of logs and 14 days retention)

### Step 2: Download the Repository
```bash
git clone <repository-url>
cd Loki-demo
```

### Step 3: Configure Grafana Cloud Data Source
1. **Log into your Grafana Cloud account**
2. **Go to Connections**
3. **Search for "loki"** - select the one under "Data sources" (not under "Integrations")
4. **Click "Data source connections"**
5. **Select the connection named "Grafanacloud-[name]-logs"**
6. **Verify the URL and username**:
   - Save the username for later use
   - Ensure the URL is `https://logs-prod-025.grafana.net` (or similar)
   - If the URL is different, you'll need to update the config later
   - **Note**: Don't worry about the "reset" button - we'll get the token in the next step

### Step 4: Install Grafana Alloy
1. **In Grafana Cloud**:
   - Go to **Connections**
   - Click **Collectors**
   - Click **"Install Grafana Alloy"**
2. **Generate a new token** and save it somewhere secure
3. **Copy the installation command** and run it in Command Prompt
4. **Verify installation**: Alloy should be installed in `C:/Programs/GrafanaLabs`
5. **Configure Alloy**:
   - Copy the `alloy.config` file from this repository
   - Replace `username` with your Grafana Cloud username
   - Replace `token från när du laddade ner alloy` with the token you generated
   - Save
   - Go to "services" and search for alloy -> restart. (You have to restart the alloy services each time you make a change to the config)

### Step 5: Update Configuration
1. **Edit `config.json`** in the project root:
   ```json
   {
     "lokiAlloyUrl": "http://localhost:1337",
     "lokiCloudUrl": "https://logs-prod-025.grafana.net/loki/api/v1/push",
     "grafanaUsername": "your-username-here",
     "grafanaPassword": "your-token-here",
     "eventLogSource": "LokiDemoApp",
     "logFilePath": "C:\\GrafanaLogs\\csharp-demo.log"
   }
   ```
2. **Verify the URL matches** what you saw in Step 3

### Step 6: Build and Run
```bash
dotnet restore
dotnet build
dotnet run
```

### Step 7: Test the Connection
If you encounter issues, run the test script:
```powershell
.\test-grafana-connection.ps1
```

### Step 8: Verify Logs in Grafana
1. **Go to Grafana Cloud Explore**
2. **Select the Loki data source** named "Grafanacloud-[name]-logs" (not Prometheus)
3. **Check for labels** - if logs are being received, you should see available label options
4. **If no logs appear**, run the application again and check for errors

## 📊 Viewing Logs

### Grafana Cloud
1. Log into your Grafana Cloud account
2. Go to **Explore**
3. Select the **Loki data source** (Grafanacloud-[name]-logs)
4. Use queries like:
   ```
   {app="LokiDemo"}
   {method="alloy-http"}
   {level="Error"}
   {env="cloud"}
   ```

### File Logging
Logs are also written to the configured file path (default: `C:\GrafanaLogs\csharp-demo.log`).

### Windows Event Viewer
1. Open **Event Viewer** (Windows + R, type `eventvwr.msc`)
2. Navigate to **Windows Logs** → **Application**
3. Look for entries with **Source**: `LokiDemoApp`

## 🔧 Troubleshooting

### Common Issues

#### "Connection refused" when using Alloy
- **Solution**: Ensure Alloy is running
- **Check**: Verify Alloy is installed in `C:/Programs/GrafanaLabs`
- **Start Alloy**: Run the Alloy executable if it's not running

#### "Authentication failed" with Grafana Cloud
- **Solution**: Verify your username and token in `config.json`
- **Check**: Ensure the token is the one you generated when installing Alloy
- **Alternative**: Run the test script to diagnose connection issues

#### "Directory not found" for file logging
- **Solution**: The application will create the directory automatically
- **Check**: Ensure the application has write permissions to `C:\GrafanaLogs`

#### No logs appearing in Grafana
- **Check**: Verify you're looking at the correct Loki data source
- **Verify**: Run the test script to ensure connectivity
- **Debug**: Check the application console for error messages

### Debug Mode
The application includes debug output. Look for lines starting with `DEBUG:` for troubleshooting information.

## 📁 Project Structure

```
Loki-demo/
├── Program.cs              # Main application logic
├── config.json             # Configuration file
├── alloy.config            # Alloy configuration
├── Loki-demo.csproj        # Project file
├── README.md               # This file
├── test-grafana-connection.ps1  # Connection test script
├── test-alloy-connection.ps1    # Alloy connection test
└── bin/                    # Build output
```

## 🧪 Testing Connections

The project includes PowerShell scripts for testing connections:

- `test-grafana-connection.ps1`: Tests direct Grafana Cloud connectivity
- `test-alloy-connection.ps1`: Tests local Alloy connectivity

## 📚 Learning Resources

- [Grafana Alloy Documentation](https://grafana.com/docs/grafana-cloud/agent/)
- [Grafana Loki Documentation](https://grafana.com/docs/loki/)
- [Grafana Cloud Documentation](https://grafana.com/docs/grafana-cloud/)
- [Serilog Documentation](https://serilog.net/)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

If you encounter issues:

1. Check the troubleshooting section above
2. Review the debug output in the console
3. Run the test scripts to diagnose connection issues
4. Check the Grafana Cloud status page
5. Open an issue in the repository

---

**Happy Logging! 🎉** 
