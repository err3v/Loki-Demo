using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text.Json;
using System.Diagnostics;

// Read configuration from JSON file and deserialize into strongly-typed Config object
var configText = File.ReadAllText("config.json");
var config = JsonSerializer.Deserialize<Config>(configText);

if (config == null)
{
    throw new InvalidOperationException("Failed to load configuration from config.json");
}

Console.WriteLine("=== Loki Demo - Choose Logging Method ===");
Console.WriteLine($"1. Log to file ({config.logFilePath})");
Console.WriteLine($"2. Log directly to Alloy ({config.lokiAlloyUrl})");
Console.WriteLine($"3. Log to Grafana Cloud via Serilog ({config.lokiCloudUrl})");
Console.Write("Enter your choice (1, 2, 3 or 4): ");

var choice = Console.ReadLine();

Console.Write("How many logs do you want to generate? (default: 10): ");
var logCountInput = Console.ReadLine();
// Parse user input with fallback to default value of 10 if parsing fails
var logCount = int.TryParse(logCountInput, out var count) ? count : 10;

Console.WriteLine($"Will generate {logCount} logs...");

try
{
    // Enable Serilog SelfLog for debugging sink issues (helps detect 404, auth errors, etc.)
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    
    // Initialize a basic Serilog logger for console output during setup
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console() // Always keep console output for debugging
        .CreateLogger();

    switch (choice)
    {
        case "1":
            await LogToFile(logCount);
            break;
        case "2":
            await LogToAlloy(logCount);
            break;
        case "3":
            await LogToGrafanaCloud(logCount);
            break;
        default:
            Console.WriteLine("Invalid choice. Using file logging as default.");
            await LogToFile(logCount);
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("Demo finished. Press any key to exit.");
Console.ReadKey();

/// <summary>
/// Logs realistic business data to a file with rolling intervals.
/// This method demonstrates file-based logging with automatic log rotation.
/// </summary>
/// <param name="logCount">Number of logs to generate</param>
async Task LogToFile(int logCount)
{
    var logFilePath = config.logFilePath;
    
    // Ensure the directory structure exists before writing files
    // This prevents "Directory not found" exceptions
    var directory = Path.GetDirectoryName(logFilePath);
    if (!Directory.Exists(directory))
    {
        Directory.CreateDirectory(directory!);
    }

    var jsonlPath = Path.Combine(
        directory,
        Path.GetFileNameWithoutExtension(logFilePath) + ".jsonl"
    );
    
    // Configure Serilog with multiple sinks (output destinations)
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information() // Only log Information level and above
        .Enrich.With(new NormalizedLevelEnricher())
        .WriteTo.Console() // Output to console for immediate feedback
        //Human readable
        .WriteTo.File(
            path: logFilePath,
            rollingInterval: RollingInterval.Day, // Create new file each day
            retainedFileCountLimit: 7, // Keep only 7 days of log files
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        )
        // 2) JSON Lines for Alloy
        .WriteTo.File(
            formatter: new RenderedCompactJsonFormatter(), // inkluderar @t, @m + properties
            path: jsonlPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7
        )
        .CreateLogger();

    Log.Information(" .NET-demo starting - writing logs to {TextPath} and JSONL to {JsonPath}", logFilePath, jsonlPath);
    
    await GenerateRealisticLogs("file", logCount);
    
    Log.Information("File logging demo completed successfully");
    // Flush any buffered logs and close the logger properly
    Log.CloseAndFlush();
    
    Console.WriteLine($"Text:  {logFilePath}");
    Console.WriteLine($"JSONL: {jsonlPath}");
}

/// <summary>
/// Logs realistic business data directly to Alloy via HTTP API.
/// This method demonstrates sending logs to Grafana Loki/Alloy for centralized log aggregation.
/// </summary>
/// <param name="logCount">Number of logs to generate</param>
async Task LogToAlloy(int logCount)
{
    Console.WriteLine($"DEBUG: Configuring Serilog to send to: {config.lokiAlloyUrl}");
    
    // Validate configuration before attempting to connect
    if (string.IsNullOrEmpty(config.lokiAlloyUrl))
    {
        Console.WriteLine("❌ ERROR: Alloy URL is missing!");
        Console.WriteLine("   Please check your config.json file and ensure lokiAlloyUrl is set.");
        Console.WriteLine("   Example: http://localhost:1337");
        return;
    }
    
    if (!config.lokiAlloyUrl.StartsWith("http://") && !config.lokiAlloyUrl.StartsWith("https://"))
    {
        Console.WriteLine("❌ ERROR: Invalid Alloy URL!");
        Console.WriteLine("   The lokiAlloyUrl should start with 'http://' or 'https://'");
        Console.WriteLine("   Example: http://localhost:1337");
        return;
    }
    
    try
    {
        // Configure Serilog with Loki/Alloy sink for remote log aggregation
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information() // Capture all log levels including Debug
            .Enrich.FromLogContext() // Add contextual information from LogContext
            .Enrich.WithProperty("ALabel", "ALabelValue") // Add custom property to all log entries
            .WriteTo.Console() // Keep console output for local debugging
            .WriteTo.GrafanaLoki(
                uri: config.lokiAlloyUrl, // Loki/Alloy endpoint URL
                credentials: null, // No authentication for local Alloy instance
                labels: new List<LokiLabel>
                {
                    // Labels help organize and filter logs in Grafana
                    new() { Key = "app", Value = "LokiDemo" }, // Application identifier
                    new() { Key = "env", Value = "local" }, // Environment
                    new() { Key = "source", Value = "csharp-demo" }, // Source system
                    new() { Key = "method", Value = "direct-alloy" } // Logging method
                },
                propertiesAsLabels: new[] { "level" },
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug // Minimum level to send to Loki
            )
            .CreateLogger();

        Console.WriteLine("DEBUG: Serilog configuration successful");
        Console.WriteLine("💡 TIP: If you see connection errors below, make sure Alloy is running");
        Console.WriteLine("   and accessible at the configured URL.");
        
        Log.Information("🎉 .NET-demo starting - sending realistic business logs to Alloy at {LokiAlloyUrl}", config.lokiAlloyUrl);
        
        await GenerateRealisticLogs("alloy", logCount);
        
        Log.Information("Direct Alloy logging demo completed successfully");
        Log.CloseAndFlush(); // Ensure all logs are sent before closing
        
        Console.WriteLine($"DEBUG: Log.CloseAndFlush() completed");
        Console.WriteLine($"Logs sent to: {config.lokiAlloyUrl}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DEBUG: Error in LogToAlloy: {ex.Message}");
        Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
        throw;
    }
}

/// <summary>
/// Logs realistic business data to Grafana Cloud via Serilog sink.
/// This method demonstrates sending logs to Grafana Cloud using the Serilog.Sinks.Grafana.Loki package
/// with proper LokiCredentials for authentication.
/// </summary>
/// <param name="logCount">Number of logs to generate</param>
async Task LogToGrafanaCloud(int logCount)
{
    Console.WriteLine($"DEBUG: Configuring Serilog to send to Grafana Cloud: {config.lokiCloudUrl}");
    
    // Validate configuration before attempting to connect
    if (string.IsNullOrEmpty(config.grafanaUsername) || string.IsNullOrEmpty(config.grafanaPassword))
    {
        Console.WriteLine("❌ ERROR: Grafana Cloud credentials are missing!");
        Console.WriteLine("   Please check your config.json file and ensure:");
        Console.WriteLine("   - grafanaUsername is set to your Grafana Cloud stack ID");
        Console.WriteLine("   - grafanaPassword is set to your Grafana Cloud API key");
        Console.WriteLine("   See README.md for setup instructions.");
        return;
    }
    
    if (!config.lokiCloudUrl.StartsWith("https://"))
    {
        Console.WriteLine("❌ ERROR: Invalid Grafana Cloud URL!");
        Console.WriteLine("   The lokiCloudUrl should start with 'https://'");
        Console.WriteLine("   Example: https://logs-prod-xxx.grafana.net");
        return;
    }
    
    if (config.lokiCloudUrl.Contains("xxx"))
    {
        Console.WriteLine("❌ ERROR: You forgot to change the url");
        Console.WriteLine("   Example: https://logs-prod-025.grafana.net");
        return;
    }
    
    try
    {
        // Create Grafana Cloud credentials using stack ID and API key
        var credentials = new Serilog.Sinks.Grafana.Loki.LokiCredentials
        {
            Login = config.grafanaUsername,     // Stack ID (e.g., "123456")
            Password = config.grafanaPassword  // API key
        };

        // Configure Serilog with Grafana Cloud Loki sink for remote log aggregation
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information() // Capture all log levels including Debug
            .Enrich.FromLogContext() // Add contextual information from LogContext
            .Enrich.WithProperty("ALabel", "ALabelValue") // Add custom property to all log entries
            .WriteTo.Console() // Keep console output for local debugging
            .WriteTo.GrafanaLoki(
                uri: config.lokiCloudUrl, // Use base URL without /loki/api/v1/push
                credentials: credentials, // Proper Grafana Cloud authentication
                labels: new List<LokiLabel>
                {
                    // Labels help organize and filter logs in Grafana Cloud
                    new() { Key = "app", Value = "LokiDemo" }, // Application identifier
                    new() { Key = "env", Value = "local" }, // Environment
                    new() { Key = "source", Value = "csharp-demo" }, // Source system
                    new() { Key = "method", Value = "serilog-grafana-cloud" } // Logging method
                },
                propertiesAsLabels: new[] { "level" },
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug // Minimum level to send to Loki
            )
            .CreateLogger();

        Console.WriteLine("DEBUG: Serilog Grafana Cloud configuration successful");
        Console.WriteLine("💡 TIP: If you see any error messages below (like 404, 401, etc.),");
        Console.WriteLine("   check your Grafana Cloud credentials and URL in config.json");
        Console.WriteLine("   The SelfLog will show detailed error information.");

        Log.Information("🎉 .NET-demo starting - sending realistic business logs to Grafana Cloud via Serilog at {LokiCloudUrl}", config.lokiCloudUrl);

        await GenerateRealisticLogs("serilog-grafana-cloud", logCount);

        Log.Information("Grafana Cloud Serilog logging demo completed successfully");

        Log.CloseAndFlush(); // Ensure all logs are sent before closing

        Console.WriteLine($"Logs sent to Grafana Cloud via Serilog: {config.lokiCloudUrl}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DEBUG: Error in LogToGrafanaCloud: {ex.Message}");
        Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
        throw;
    }
}

#region EventViewer
// Fick inte denna att fungera på ett tillräckligt snyggt sätt. Bortse från denna. Kommer att fixa i framtiden om intrsse finns. 
/// <summary>
/// Logs realistic business data to Windows Event Viewer.
/// This method demonstrates Windows-specific logging for system administrators.
/// </summary>
/// <param name="logCount">Number of logs to generate</param>
async Task LogToEventViewer(int logCount)
{
    Console.WriteLine($"DEBUG: Configuring Serilog to send to Windows Event Viewer (Source: {config.eventLogSource})");
    try
    {
        // Configure Serilog with Windows Event Log sink
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information() // Capture all log levels
            .Enrich.FromLogContext() // Add contextual information
            .Enrich.WithProperty("level", "info") // Add custom property
            .WriteTo.Console() // Keep console output
            .WriteTo.EventLog(
                source: config.eventLogSource, // Event source name
                manageEventSource: true, // Let Serilog manage the event source
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug // Minimum level to log
            )
            .CreateLogger();

        Console.WriteLine("DEBUG: Serilog EventLog configuration successful");
        Console.WriteLine($"🎉 .NET-demo starting - sending realistic business logs to Windows Event Viewer (Source: {config.eventLogSource})");
        await GenerateRealisticLogs("eventlog", logCount);
        Console.WriteLine("Event Viewer logging demo completed successfully");
        Log.CloseAndFlush();
        Console.WriteLine($"DEBUG: Log.CloseAndFlush() completed");
        Console.WriteLine($"Logs sent to Windows Event Viewer (Source: {config.eventLogSource})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DEBUG: Error in LogToEventViewer: {ex.Message}");
        Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
        throw;
    }
}

void LogEventlog(string operation, string entity, string entityId, string userId, int elapsedTime, string status, string logLevel)
{
    var jsonModel = new
    {
        level = logLevel,
        Operation = operation,
        Entity = entity,
        EntityId = entityId,
        UserId = userId,
        ElapsedTime = elapsedTime,
        Status = status
    };

    var json = JsonSerializer.Serialize(jsonModel, new JsonSerializerOptions
    {
        WriteIndented = false
    });

    using (var eventLog = new EventLog())
    {
        eventLog.Source = config.eventLogSource;
        // Use structured logging with named parameters for better querying and filtering
        switch (logLevel)
        {
            case "info":
                eventLog.WriteEntry(json, EventLogEntryType.Information);
                break;
            case "warn":
                eventLog.WriteEntry(json, EventLogEntryType.Warning);
                break;
            case "error":
                eventLog.WriteEntry(json, EventLogEntryType.Error);
                break;
        }
    }
}
#endregion EventViewer

/// <summary>
/// Determines the log level based on a random roll with 90% Info, 5% Warning, 5% Error distribution.
/// This simulates realistic log level distribution in production systems.
/// </summary>
/// <param name="random">Random number generator</param>
/// <returns>The determined log level</returns>
string DetermineLogLevel(Random random)
{
    var roll = random.Next(1, 101); // Generate number 1-100
    return roll switch
    {
        <= 90 => "info",  // 90% chance (rolls 1-90)
        <= 95 => "warn",      // 5% chance (rolls 91-95)
        _ => "error"             // 5% chance (rolls 96-100)
    };
}

/// <summary>
/// Logs a business operation with the specified level and details.
/// This method demonstrates structured logging with consistent parameter naming.
/// </summary>
/// <param name="operation">The operation being performed (Create, Update, etc.)</param>
/// <param name="entity">The entity type (Contact, Account, etc.)</param>
/// <param name="entityId">Unique entity identifier (GUID)</param>
/// <param name="userId">User performing the operation</param>
/// <param name="elapsedTime">Operation duration in milliseconds</param>
/// <param name="status">Operation status (Success, Failed, etc.)</param>
/// <param name="logLevel">Log level to use (Information, Warning, Error)</param>
void LogBusinessOperation(string operation, string entity, string entityId, string userId, int elapsedTime, string status, string logLevel)
{
    // Use structured logging with named parameters for better querying and filtering
    switch (logLevel)
    {
        case "info":
            Log.Information("Operation {Operation}, Entity: {Entity}, EntityId: {EntityId}, UserId: {UserId}, ElapsedTime: {ElapsedTime}ms, Status: {Status}",
                operation, entity, entityId, userId, elapsedTime, status);
            break;
        case "warn":
            Log.Warning("Operation {Operation}, Entity: {Entity}, EntityId: {EntityId}, UserId: {UserId}, ElapsedTime: {ElapsedTime}ms, Status: {Status}",
                operation, entity, entityId, userId, elapsedTime, status);
            break;
        case "error":
            Log.Error("Operation {Operation}, Entity: {Entity}, EntityId: {EntityId}, UserId: {UserId}, ElapsedTime: {ElapsedTime}ms, Status: {Status}",
                operation, entity, entityId, userId, elapsedTime, status);
            break;
    }
}

/// <summary>
/// Generates realistic business logs with random entities, operations, and performance data.
/// This simulates a real business application with various operations and users.
/// </summary>
/// <param name="method">The logging method being used (file, alloy, eventlog)</param>
/// <param name="count">Number of logs to generate</param>
async Task GenerateRealisticLogs(string method, int count)
{
    var random = new Random();

    // Predefined arrays of business entities and operations to simulate realistic scenarios
    var ENTITIES = new[] { "Contact", "Account", "Opportunity", "Lead", "Case", "Product", "Order", "Invoice" };
    var OPERATIONS = new[] { "Create", "Update", "Delete", "Read", "Search", "Export", "Import", "Validate", "Process", "Archive" };
    var USERS = new[] { "john.doe", "jane.smith", "bob.wilson", "alice.johnson", "charlie.brown", "diana.prince", "bruce.wayne", "peter.parker" };
    var STATUSES = new[] { "Success", "Failed", "Pending", "Cancelled", "Completed", "Error", "Warning" };

    Console.WriteLine($"Generating {count} realistic business logs...");

    for (var i = 1; i <= count; i++)
    {
        // Generate random business data for each log entry
        var entity = ENTITIES[random.Next(ENTITIES.Length)];
        var operation = OPERATIONS[random.Next(OPERATIONS.Length)];
        var user = USERS[random.Next(USERS.Length)];
        var status = STATUSES[random.Next(STATUSES.Length)];
        var entityId = Guid.NewGuid().ToString(); // Generate unique identifier
        var userId = Guid.NewGuid().ToString(); // Generate unique user identifier
        var elapsedTime = random.Next(50, 2000); // Random duration between 50ms and 2 seconds

        // Generate log levels with realistic distribution (90% Info, 5% Warning, 5% Error)
        var logLevel = DetermineLogLevel(random);

        //If (method = eventlog) { LogEventlog(operation, entity, entityId, userId, elapsedTime, status, logLevel); }

        // Log the business operation with structured data
        LogBusinessOperation(operation, entity, entityId, userId, elapsedTime, status, logLevel);

        // Show progress for large numbers to keep user informed
        if (count > 20 && i % 10 == 0)
        {
            Console.WriteLine($"Progress: {i}/{count} logs generated...");
        }

        // Add realistic delay between operations to simulate real application behavior
        await Task.Delay(random.Next(200, 800)); // Random delay between 200-800ms
    }

    Console.WriteLine($"Generated {count} logs successfully!");
}

/// <summary>
/// Configuration record for application settings.
/// Using C# records for immutable configuration with automatic property generation.
/// </summary>
/// <param name="lokiAlloyUrl">URL for Alloy Loki endpoint (local Grafana Loki instance)</param>
/// <param name="lokiCloudUrl">URL for Grafana Cloud Loki endpoint (cloud-hosted)</param>
/// <param name="grafanaUsername">Username for Grafana Cloud authentication</param>
/// <param name="grafanaPassword">Password for Grafana Cloud authentication</param>
/// <param name="eventLogSource">Source name for Windows Event Log (must be registered)</param>
/// <param name="logFilePath">Path for file logging (supports rolling files)</param>
public record Config(
    string lokiAlloyUrl,
    string lokiCloudUrl,
    string grafanaUsername,
    string grafanaPassword,
    string eventLogSource,
    string logFilePath
);

sealed class NormalizedLevelEnricher : ILogEventEnricher
{
    static string Map(LogEventLevel l) => l switch
    {
        LogEventLevel.Verbose => "trace",
        LogEventLevel.Debug => "debug",
        LogEventLevel.Information => "info",
        LogEventLevel.Warning => "warn",
        LogEventLevel.Error => "error",
        LogEventLevel.Fatal => "fatal",
        _ => l.ToString().ToLowerInvariant()
    };
    public void Enrich(LogEvent e, ILogEventPropertyFactory pf) =>
        e.AddOrUpdateProperty(pf.CreateProperty("level", Map(e.Level)));
}
