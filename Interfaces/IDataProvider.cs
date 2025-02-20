using System.Data;

namespace MoE.Commercial.Data
{
    /// <summary>
    /// Defines the interface for a data provider that can connect to a database. This should move into a shared library.
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>
        /// Executes a SQL statement and returns an enumeration of type T.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> Query<T>(string query, GenericDbParameter[]? parameters = null);

        /// <summary>
        /// Executes a SQL statement and returns a DataTable.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        Task<DataTable> ExecuteSql(string sql, GenericDbParameter[]? parameters = null);

        /// <summary>
        /// Executes a command with no result set.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task ExecuteNonQuery(
            string commandText,
        CommandType commandType,
            GenericDbParameter[] parameters
        );

        Task<T?> ExecuteScalar<T>(string sql, GenericDbParameter[]? parameters = null);

        /// <summary>
        /// The schema used by the provider (if any)
        /// </summary>
        string Schema { get; }
    }
}