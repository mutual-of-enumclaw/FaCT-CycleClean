using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fact.BatchCleaner.Logging
{
    /// <summary>
    /// Extension methods for configuring file logging in the application.
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Adds file logging to the logging builder with the specified settings.
        /// </summary>
        /// <param name="builder">The logging builder to configure</param>
        /// <param name="settings">The logging settings to use</param>
        /// <returns>The logging builder for method chaining</returns>
        public static ILoggingBuilder AddFileLogging(this ILoggingBuilder builder, LoggingSettings settings)
        {
            return builder.AddProvider(new FileLoggerProvider(Options.Create(settings)));
        }

        /// <summary>
        /// Adds file logging to the logging builder using settings from configuration.
        /// </summary>
        /// <param name="builder">The logging builder to configure</param>
        /// <param name="configureSettings">Action to configure the logging settings</param>
        /// <returns>The logging builder for method chaining</returns>
        public static ILoggingBuilder AddFileLogging(this ILoggingBuilder builder, Action<LoggingSettings> configureSettings)
        {
            var settings = new LoggingSettings();
            configureSettings(settings);
            return builder.AddFileLogging(settings);
        }
    }
}