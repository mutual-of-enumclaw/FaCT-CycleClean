using Dapper;
using Microsoft.Extensions.Options;
using MoE.Commercial.Data.Extensions;
using System.Data;
using System.Data.Odbc;

namespace MoE.Commercial.Data.Db2
{
    /// <summary>
    /// Provides data access functionality for IBM DB2 databases using ODBC connections.
    /// This class implements the IDataProvider interface to offer standardized database operations
    /// with proper async/await support and connection management.
    /// </summary>
    public class Db2OdbcDataProvider : IDataProvider
    {
        private readonly Db2Settings _config;

        /// <summary>
        /// Initializes a new instance of the Db2OdbcDataProvider class.
        /// </summary>
        /// <param name="options">Configuration options containing connection string and schema information</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
        public Db2OdbcDataProvider(IOptions<Db2Settings> options)
        {
            _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            if (string.IsNullOrWhiteSpace(_config.ConnectionString))
                throw new ArgumentException("Connection string is required", nameof(options));
        }

        /// <summary>
        /// Gets the database configuration settings.
        /// </summary>
        public Db2Settings Config => _config;

        /// <summary>
        /// Gets the database schema name to use for queries.
        /// </summary>
        public string Schema => _config.Schema ?? "";

        /// <summary>
        /// Executes a SQL query and returns a collection of strongly-typed objects.
        /// </summary>
        /// <typeparam name="T">The type to map the query results to</typeparam>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>A collection of objects of type T</returns>
        /// <exception cref="ArgumentException">Thrown when query is null or empty</exception>
        /// <exception cref="System.Data.Common.DbException">Thrown when database operations fail</exception>
        public async Task<IEnumerable<T>> Query<T>(string query, GenericDbParameter[]? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty", nameof(query));

            var dynamicParameters = new DynamicParameters();

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    dynamicParameters.Add(param.ParameterName, param.Value);
                }
            }

            using var connection = new OdbcConnection(_config.ConnectionString);
            await connection.OpenAsync();
            
            // Set command timeout to 10 minutes to match other methods
            return await connection.QueryAsync<T>(query, dynamicParameters, commandTimeout: 600);
        }

        /// <summary>
        /// Executes a command that does not return a result set (INSERT, UPDATE, DELETE).
        /// </summary>
        /// <param name="commandText">The SQL command to execute</param>
        /// <param name="commandType">The type of command (Text, StoredProcedure, etc.)</param>
        /// <param name="parameters">Parameters for the command</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <exception cref="ArgumentException">Thrown when commandText is null or empty</exception>
        /// <exception cref="ArgumentNullException">Thrown when parameters is null</exception>
        /// <exception cref="System.Data.Common.DbException">Thrown when database operations fail</exception>
        public async Task ExecuteNonQuery(
            string commandText,
            CommandType commandType,
            GenericDbParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentException("Command text cannot be null or empty", nameof(commandText));
            
            ArgumentNullException.ThrowIfNull(parameters);

            using var connection = new OdbcConnection(_config.ConnectionString);
            await connection.OpenAsync();

            using var command = new OdbcCommand(commandText, connection)
            {
                CommandTimeout = 600, // 10 minutes to match HTTP timeout
                CommandType = commandType
            };

            foreach (var param in parameters)
            {
                command.Parameters.Add(new OdbcParameter(param.ParameterName, param.Value ?? DBNull.Value));
            }

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Executes a SQL query and returns the results as a DataTable.
        /// </summary>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>A DataTable containing the query results</returns>
        /// <exception cref="ArgumentException">Thrown when sql is null or empty</exception>
        /// <exception cref="System.Data.Common.DbException">Thrown when database operations fail</exception>
        public async Task<DataTable> ExecuteSql(string sql, GenericDbParameter[]? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL cannot be null or empty", nameof(sql));

            using var connection = new OdbcConnection(_config.ConnectionString);
            using var command = new OdbcCommand(sql, connection)
            {
                CommandTimeout = 600 // 10 minutes to match HTTP timeout
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(new OdbcParameter(param.ParameterName, param.Value ?? DBNull.Value));
                }
            }

            using var adapter = new OdbcDataAdapter(command);
            var dataSet = new DataSet();
            
            // Fill is synchronous, but we're wrapping in Task.Run to maintain async pattern
            await Task.Run(() => adapter.Fill(dataSet));
            
            return dataSet.Tables.Count > 0 ? dataSet.Tables[0] : new DataTable();
        }

        /// <summary>
        /// Executes a SQL query and returns a single scalar value.
        /// </summary>
        /// <typeparam name="T">The type to convert the result to</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>The scalar result converted to type T, or default(T) if null</returns>
        /// <exception cref="ArgumentException">Thrown when sql is null or empty</exception>
        /// <exception cref="System.Data.Common.DbException">Thrown when database operations fail</exception>
        public async Task<T?> ExecuteScalar<T>(string sql, GenericDbParameter[]? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL cannot be null or empty", nameof(sql));

            using var connection = new OdbcConnection(_config.ConnectionString);
            using var command = new OdbcCommand(sql, connection)
            {
                CommandTimeout = 600 // 10 minutes to match HTTP timeout
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(new OdbcParameter(param.ParameterName, param.Value ?? DBNull.Value));
                }
            }

            await connection.OpenAsync();
            var rawData = await command.ExecuteScalarAsync();

            if (rawData == null || rawData == DBNull.Value)
                return default(T);

            // Handle string to other type conversions
            if (rawData is string stringValue
                && typeof(T) != typeof(string)
                && !typeof(T).IsSubclassOf(typeof(string)))
            {
                return stringValue.Convert<T>();
            }

            return rawData is T result ? result : default(T);
        }
    }
}