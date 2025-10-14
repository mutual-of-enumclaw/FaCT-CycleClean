using System.Threading.Tasks;

namespace Fact.BatchCleaner
{
    /// <summary>
    /// Defines the contract for the cycle cleaning service that processes insurance policies.
    /// This service is responsible for executing the main policy cleaning workflow,
    /// which includes retrieving policies from the database and processing them through
    /// the required XML operations.
    /// </summary>
    public interface ICycleCleaner
    {
        /// <summary>
        /// Executes the main policy cleaning process.
        /// This method orchestrates the entire workflow including:
        /// - Retrieving policies from the database
        /// - Processing each policy through three XML actions (Delete, Set RNT Reason, Create RNT Action)
        /// - Generating comprehensive reports of the processing results
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        /// <exception cref="System.Data.Common.DbException">Thrown when database operations fail</exception>
        /// <exception cref="HttpRequestException">Thrown when HTTP communication fails</exception>
        Task Run();
    }
}
