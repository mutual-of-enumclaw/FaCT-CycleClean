namespace Fact.BatchCleaner
{
    /// <summary>
    /// Configuration settings for logging behavior in the FaCT Cycle Cleaner application.
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// Gets or sets the directory where log files should be stored.
        /// </summary>
        public string LogDirectory { get; set; } = "Logs";

        /// <summary>
        /// Gets or sets the log file name pattern. Use {Date} placeholder for date formatting.
        /// </summary>
        public string LogFileName { get; set; } = "CycleCleaner_{Date}.log";

        /// <summary>
        /// Gets or sets whether verbose logging (including debug messages) should be enabled.
        /// When false, only policy processing and summary messages are logged.
        /// </summary>
        public bool VerboseLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets whether timestamps should be included in log messages.
        /// </summary>
        public bool IncludeTimestamps { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of days to retain log files before automatic cleanup.
        /// </summary>
        public int RetainDays { get; set; } = 30;

        /// <summary>
        /// Static property to hold the log file path established at application startup.
        /// This ensures one log file per application run.
        /// </summary>
        public static string? ApplicationLogFilePath { get; set; }

        /// <summary>
        /// Gets the full log file path with date and time formatting applied.
        /// This creates a unique log file for each application run by using the static path
        /// established at startup, or creates a new one if not yet set.
        /// </summary>
        /// <returns>Complete path to the current log file</returns>
        public string GetLogFilePath()
        {
            // If we already have an established log file path for this run, use it
            if (!string.IsNullOrEmpty(ApplicationLogFilePath))
            {
                return ApplicationLogFilePath;
            }

            // Create a new log file path based on the current time (this should only happen once per run)
            var fileName = LogFileName.Replace("{Date}", DateTime.Now.ToString("yyyyMMddHHmm"));
            var fullPath = Path.Combine(LogDirectory, fileName);
            
            // Store this path statically so all subsequent calls use the same file
            ApplicationLogFilePath = fullPath;
            
            // Debug output to console to verify path generation
            Console.WriteLine($"[DEBUG] Generated log file path for this run: {fullPath}");
            Console.WriteLine($"[DEBUG] Log directory exists: {Directory.Exists(LogDirectory)}");
            
            return fullPath;
        }
    }
}