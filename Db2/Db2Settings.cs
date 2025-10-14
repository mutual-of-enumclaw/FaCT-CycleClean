namespace MoE.Commercial.Data
{
    /// <summary>
    /// Configuration settings for DB2 database connections and related services.
    /// This class is used with the Options pattern to configure database connectivity
    /// and communication framework settings.
    /// </summary>
    public class Db2Settings
    {
        /// <summary>
        /// Gets or sets the name identifier for this database configuration.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database connection string used to connect to the DB2 database.
        /// This should include all necessary connection parameters such as server, database,
        /// credentials, and driver information.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default database schema to use for queries.
        /// If not specified, queries will need to include explicit schema names.
        /// </summary>
        public string Schema { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the communication framework settings for external service integration.
        /// </summary>
        public CommFrameworkSettings? CommFramework { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of policies to process in a single batch.
        /// This limits the number of records returned by the policy query.
        /// Set to 0 or negative value to disable the limit.
        /// This value should be configured in appsettings.json.
        /// </summary>
        public int MaxPolicyBatchSize { get; set; }
    }

    /// <summary>
    /// Configuration settings for the communication framework used to send XML requests
    /// to external systems such as the POINT system.
    /// </summary>
    public class CommFrameworkSettings
    {
        /// <summary>
        /// Gets or sets the URI endpoint where XML requests should be sent.
        /// This is typically the URL of the communication framework servlet or API.
        /// </summary>
        public string RequestUri { get; set; } = string.Empty;
    }
}
