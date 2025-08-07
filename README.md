# Loki Demo - .NET Logging Application

A comprehensive .NET demo application that demonstrates different logging methods and integrations with Grafana Loki/Alloy. This project showcases file logging, direct Loki/Alloy integration, and Windows Event Log logging.

## 🚀 Features

- **Multiple Logging Methods**: File, Grafana Loki/Alloy, and Windows Event Log
- **Realistic Business Data**: Generates realistic business operation logs
- **Structured Logging**: Uses Serilog with structured data for better querying
- **Configurable**: Easy configuration via JSON file
- **Cross-Platform**: Works on Windows, Linux, and macOS

## 📋 Prerequisites

- .NET 9.0 SDK or later
- Grafana Alloy (for local Loki testing) or Grafana Cloud account
- Windows (for Event Log functionality)

## 🛠️ Installation

1. **Clone the repository**:
   ```bash
   git clone <repository-url>
   cd Loki-demo
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build the project**:
   ```bash
   dotnet build
   ```

## ⚙️ Configuration

### 1. Grafana Cloud Setup (Recommended for beginners)

**Important Note**: For the most reliable Grafana Cloud integration, use option 2 (Alloy) which acts as a local proxy and handles authentication automatically. The direct Grafana Cloud integration (option 4) may have authentication limitations.

#### Finding Your Grafana Cloud Credentials

1. **Sign up for Grafana Cloud**:
   - Go to [Grafana Cloud](https://grafana.com/auth/sign-up/create-user)
   - Create a free account (includes 3GB of logs and 14 days retention)

2. **Get your Loki URL**:
   - Log into your Grafana Cloud account
   - Go to **My Account** → **Access Policies**
   - Click on your stack (usually named something like "your-username-grafana")
   - In the **Details** tab, find the **Logs** section
   - Copy the **Loki URL** (format: `https://logs-prod-xxx.grafana.net`)

3. **Generate API Key**:
   - In the same stack details page, go to **API Keys** tab
   - Click **Add API key**
   - Give it a name like "Loki Demo App"
   - Set **Role** to **MetricsPublisher** (for logs)
   - Set **Expiration** as needed
   - Click **Create**
   - **Copy the API key** (you won't see it again!)

4. **Update config.json**:
   ```json
   {
     "lokiCloudUrl": "https://logs-prod-xxx.grafana.net",
     "grafanaUsername": "your-username",
     "grafanaPassword": "your-api-key-here"
   }
   ```

### 2. Local Alloy Setup (Advanced)

If you want to run Grafana Alloy locally:

1. **Install Alloy**:
   ```bash
   # Download from https://grafana.com/docs/grafana-cloud/agent/static/flow/download/
   # Or use Docker:
   docker run -d --name alloy -p 12345:12345 grafana/agent:latest
   ```

2. **Configure Alloy** (create `alloy.config`):
   ```yaml
   server:
     log_level: info
     http_listen_port: 12345

   loki:
     configs:
     - name: default
       clients:
         - url: http://localhost:3100/loki/api/v1/push
   ```

3. **Update config.json**:
   ```json
   {
     "lokiAlloyUrl": "http://localhost:12345/loki/api/v1/push"
   }
   ```

### 3. Current Configuration

The project comes with a pre-configured `config.json` that includes:
- Local Alloy URL for testing
- Grafana Cloud credentials for immediate testing
- Windows Event Log source configuration
- File logging path

### 4. Complete Configuration Example

If you want to customize the configuration, create or update `config.json` in the project root:

```json
{
  "lokiAlloyUrl": "http://localhost:12345/loki/api/v1/push",
  "lokiCloudUrl": "https://logs-prod-xxx.grafana.net",
  "grafanaUsername": "your-username",
  "grafanaPassword": "your-api-key-here",
  "eventLogSource": "LokiDemoApp",
  "logFilePath": "logs/loki-demo.log"
}
```

## 🏃‍♂️ Running the Application

1. **Run the application**:
   ```bash
   dotnet run
   ```

2. **Choose logging method**:
   - `1`: Log to file (simplest, no external dependencies)
   - `2`: Log to Alloy/Loki (recommended for Grafana Cloud integration)
   - `3`: Log to Windows Event Viewer (Windows only)
   - `4`: Log to Grafana Cloud (experimental, may require additional setup)

3. **Enter number of logs** to generate (default: 10)

## 📊 Viewing Logs

### File Logging
Logs are written to the configured file path (default: `logs/loki-demo.log`). Files are rotated daily and kept for 7 days.

### Grafana Cloud
**Recommended Approach**: Use option 2 (Alloy) which automatically forwards logs to Grafana Cloud.

**Direct Integration (Experimental)**: 
1. Log into your Grafana Cloud account
2. Go to **Explore**
3. Select **Loki** as the data source
4. Use queries like:
   ```
   {app="LokiDemo"}
   {method="grafana-cloud"}
   {level="Error"}
   {env="cloud"}
   ```

**Note**: Option 4 (direct Grafana Cloud) may not work due to authentication limitations in the current Serilog sink version. Use option 2 (Alloy) for reliable Grafana Cloud integration.

### Windows Event Viewer
1. Open **Event Viewer** (Windows + R, type `eventvwr.msc`)
2. Navigate to **Windows Logs** → **Application**
3. Look for entries with **Source**: `LokiDemoApp`

## 🔧 Troubleshooting

### Common Issues

#### "Failed to create Event Log source"
- **Solution**: Run the application as Administrator
- **Alternative**: Create the source manually in PowerShell (as admin):
  ```powershell
  New-EventLog -LogName Application -Source LokiDemoApp
  ```

#### "Connection refused" when using Alloy
- **Solution**: Ensure Alloy is running and accessible
- **Check**: Verify the URL in `config.json` matches your Alloy setup

#### "Authentication failed" with Grafana Cloud
- **Solution**: Use option 2 (Alloy) instead of option 4 for reliable Grafana Cloud integration
- **Alternative**: The Serilog.Sinks.Grafana.Loki package may have authentication limitations
- **Note**: The config.json already includes sample credentials for testing
- **Workaround**: Alloy handles Grafana Cloud authentication automatically

#### "Directory not found" for file logging
- **Solution**: The application will create the directory automatically
- **Check**: Ensure the application has write permissions to the target location

### Debug Mode
The application includes debug output. Look for lines starting with `DEBUG:` for troubleshooting information.

## 📁 Project Structure

```
Loki-demo/
├── Program.cs              # Main application logic
├── config.json             # Configuration file
├── Loki-demo.csproj        # Project file
├── README.md               # This file
├── alloy.config            # Alloy configuration (if using local setup)
├── logs/                   # Generated log files (created automatically)
└── bin/                    # Build output
```

## 🧪 Testing Connections

The project includes PowerShell scripts for testing connections:

- `test-grafana-connection.ps1`: Tests Grafana Cloud connectivity
- `test-alloy-connection.ps1`: Tests local Alloy connectivity

## 📚 Learning Resources

- [Serilog Documentation](https://serilog.net/)
- [Grafana Loki Documentation](https://grafana.com/docs/loki/)
- [Grafana Cloud Documentation](https://grafana.com/docs/grafana-cloud/)
- [Windows Event Log](https://docs.microsoft.com/en-us/windows/win32/eventlog/)

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
3. Check the Grafana Cloud status page
4. Open an issue in the repository

---

**Happy Logging! 🎉** 