using Fact.BatchCleaner;
using Fact.BatchCleaner.Logging;
using Fact.BatchCleaner.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoE.Commercial.Data;
using MoE.Commercial.Data.Db2;
using System.Net.Http;

// Test directory access first
Console.WriteLine("=== Pre-startup Directory Test ===");
DirectoryTest.TestDirectoryAccess();
Console.WriteLine("=== Directory Test Complete ===\n");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureLogging((context, logging) =>
    {
        // Clear default providers first
        logging.ClearProviders();
        
        // Get logging settings from configuration and establish the log file path once for this run
        var loggingSettings = new LoggingSettings();
        context.Configuration.GetSection("Logging:File").Bind(loggingSettings);
        
        // Initialize the static log file path for this application run
        var logFilePath = loggingSettings.GetLogFilePath();
        Console.WriteLine($"[STARTUP] Log file established for this run: {logFilePath}");
        
        // Debug the logging configuration
        Console.WriteLine($"[STARTUP DEBUG] Log Directory: {loggingSettings.LogDirectory}");
        Console.WriteLine($"[STARTUP DEBUG] Log File Name Template: {loggingSettings.LogFileName}");
        Console.WriteLine($"[STARTUP DEBUG] Verbose Logging: {loggingSettings.VerboseLogging}");
        Console.WriteLine($"[STARTUP DEBUG] Include Timestamps: {loggingSettings.IncludeTimestamps}");
        
        // Add console logging first
        logging.AddConsole(options =>
        {
            options.IncludeScopes = false;
            options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
        });
        
        // Add our custom file logging
        logging.AddFileLogging(loggingSettings);
        
        // Set the minimum level to ensure all messages get through to providers
        logging.SetMinimumLevel(LogLevel.Trace);
    })
    .ConfigureServices((context, services) =>
    {
        // Register config-bound settings using Options pattern
        services.Configure<Db2Settings>(context.Configuration.GetSection("Db2"));
        services.Configure<LoggingSettings>(context.Configuration.GetSection("Logging:File"));

        // Register HttpClient with extended timeout - more explicit configuration
        services.AddHttpClient<CycleCleaner>("CycleCleaner", (serviceProvider, client) =>
        {
            var timeout = TimeSpan.FromMinutes(20); // Increase to 20 minutes
            client.Timeout = timeout;
            
            // Log the timeout setting for verification
            var logger = serviceProvider.GetService<ILogger<Program>>();
            logger?.LogInformation("HttpClient timeout configured to {TimeoutMinutes} minutes ({TimeoutSeconds} seconds)", 
                timeout.TotalMinutes, timeout.TotalSeconds);
            
            // Set additional timeout-related headers if needed
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
        });

        services.AddTransient<ICycleCleaner>(sp =>
        {
            // Get the specifically configured HttpClient
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("CycleCleaner");
            
            var dataProvider = sp.GetRequiredService<IDataProvider>();
            var logger = sp.GetRequiredService<ILogger<CycleCleaner>>();
            var xmlBuilder = sp.GetRequiredService<IXmlMessageBuilder>();
            var db2Options = sp.GetRequiredService<IOptions<Db2Settings>>();
            var loggingOptions = sp.GetRequiredService<IOptions<LoggingSettings>>();

            // Log HttpClient timeout for verification
            logger.LogInformation("Creating CycleCleaner with HttpClient timeout: {TimeoutSeconds} seconds", 
                httpClient.Timeout.TotalSeconds);

            return new CycleCleaner(dataProvider, logger, xmlBuilder, db2Options, loggingOptions, httpClient);
        });
        
        services.AddTransient<IDataProvider, Db2OdbcDataProvider>();
        services.AddSingleton<IXmlMessageBuilder, XmlMessageBuilder>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var logger = services.GetRequiredService<ILogger<Program>>();

try
{
    // Test logging immediately to verify file logging is working
    Console.WriteLine("[MAIN] About to test logging...");
    logger.LogWarning("=== FaCT Cycle Cleaner Starting ===");
    logger.LogWarning("Application started at {StartTime}", DateTime.Now);
    
    // Log the MaxPolicyBatchSize configuration at startup
    var db2Options = services.GetRequiredService<IOptions<Db2Settings>>();
    var maxBatchSize = db2Options.Value.MaxPolicyBatchSize;
    logger.LogWarning("MaxPolicyBatchSize configured to: {MaxBatchSize} {BatchSizeNote}", 
        maxBatchSize, 
        maxBatchSize <= 0 ? "(unlimited - will process ALL matching policies)" : "policies");
    
    logger.LogWarning("This is a test warning message to verify file logging");
    logger.LogError("This is a test error message to verify file logging");
    Console.WriteLine("[MAIN] Logging tests complete, starting main application...");

    var app = services.GetRequiredService<ICycleCleaner>();
    await app.Run();

    logger.LogWarning("=== FaCT Cycle Cleaner Completed Successfully ===");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    Console.WriteLine($"[CRITICAL ERROR] {ex}");
    throw;
}
finally
{
    logger.LogWarning("Application ended at {EndTime}", DateTime.Now);
    
    // Check if we should wait for user input
    // Don't wait if:
    // 1. Running as a service or scheduled task (no console window)
    // 2. Console input has been redirected
    // 3. Application was started with --no-wait argument
    bool shouldWait = Environment.UserInteractive && 
                     !Console.IsInputRedirected && 
                     !args.Contains("--no-wait", StringComparer.OrdinalIgnoreCase);
    
    if (shouldWait)
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    else
    {
        Console.WriteLine("Application completed - exiting automatically.");
    }
}


