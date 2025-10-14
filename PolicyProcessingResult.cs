using System.Data;
using System.Text;

namespace Fact.BatchCleaner
{
    /// <summary>
    /// Represents the processing result for a single insurance policy,
    /// including the outcomes of all three XML actions performed on the policy.
    /// </summary>
    public class PolicyProcessingResult
    {
        /// <summary>
        /// Gets or sets the unique policy identifier.
        /// </summary>
        public string PolicyId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the policy symbol (line of business identifier).
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the policy number.
        /// </summary>
        public string PolicyNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the policy module number.
        /// </summary>
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the location company identifier.
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the master company identifier.
        /// </summary>
        public string MasterCompany { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time when processing started for this policy.
        /// </summary>
        public DateTime ProcessingStartTime { get; set; }

        /// <summary>
        /// Gets or sets the time when processing completed for this policy.
        /// </summary>
        public DateTime ProcessingEndTime { get; set; }
        
        /// <summary>
        /// Gets or sets the result of the delete request XML action.
        /// </summary>
        public XmlActionResult DeleteRequestResult { get; set; } = new XmlActionResult();

        /// <summary>
        /// Gets or sets the result of the set RNT reason XML action.
        /// </summary>
        public XmlActionResult SetRNTReasonResult { get; set; } = new XmlActionResult();

        /// <summary>
        /// Gets or sets the result of the create RNT action XML action.
        /// </summary>
        public XmlActionResult CreateRNTActionResult { get; set; } = new XmlActionResult();
        
        /// <summary>
        /// Gets a value indicating whether all three XML actions were successful for this policy.
        /// </summary>
        public bool AllActionsSuccessful => 
            DeleteRequestResult.IsSuccessful && 
            SetRNTReasonResult.IsSuccessful && 
            CreateRNTActionResult.IsSuccessful;
            
        /// <summary>
        /// Creates a PolicyProcessingResult instance from a database DataRow.
        /// </summary>
        /// <param name="row">DataRow containing policy information from the database</param>
        /// <returns>New PolicyProcessingResult instance with populated policy information</returns>
        /// <exception cref="ArgumentNullException">Thrown when row is null</exception>
        public static PolicyProcessingResult FromDataRow(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);
            
            string Get(string col) => row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? row[col]?.ToString() ?? "" : "";
            
            return new PolicyProcessingResult
            {
                PolicyId = Get("POL_ID"),
                Symbol = Get("SYMBOL"),
                PolicyNumber = Get("POLICY0NUM"),
                Module = Get("MODULE"),
                Location = Get("LOCATION"),
                MasterCompany = Get("MASTER0CO"),
                ProcessingStartTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Represents the result of a single XML action performed on a policy.
    /// </summary>
    public class XmlActionResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the XML action was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the name of the XML action that was performed.
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTTP status code returned from the request.
        /// </summary>
        public int HttpStatusCode { get; set; }

        /// <summary>
        /// Gets or sets any error message that occurred during the action.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the response body content from the HTTP request.
        /// </summary>
        public string ResponseBody { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time when the action was performed.
        /// </summary>
        public DateTime ActionTime { get; set; }

        /// <summary>
        /// Gets or sets the duration of the action execution.
        /// </summary>
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Provides a comprehensive summary of the policy processing operation,
    /// including statistics and detailed results for all processed policies.
    /// </summary>
    public class ProcessingSummary
    {
        /// <summary>
        /// Gets or sets the total number of policies that were processed.
        /// </summary>
        public int TotalPoliciesProcessed { get; set; }

        /// <summary>
        /// Gets or sets the number of policies that were successfully processed.
        /// </summary>
        public int SuccessfulPolicies { get; set; }

        /// <summary>
        /// Gets or sets the number of policies that failed during processing.
        /// </summary>
        public int FailedPolicies { get; set; }

        /// <summary>
        /// Gets or sets the total number of XML actions that were attempted.
        /// </summary>
        public int TotalXmlActionsAttempted { get; set; }

        /// <summary>
        /// Gets or sets the number of XML actions that were successful.
        /// </summary>
        public int SuccessfulXmlActions { get; set; }

        /// <summary>
        /// Gets or sets the number of XML actions that failed.
        /// </summary>
        public int FailedXmlActions { get; set; }

        /// <summary>
        /// Gets or sets the time when processing started.
        /// </summary>
        public DateTime ProcessingStartTime { get; set; }

        /// <summary>
        /// Gets or sets the time when processing completed.
        /// </summary>
        public DateTime ProcessingEndTime { get; set; }

        /// <summary>
        /// Gets the total duration of the processing operation.
        /// </summary>
        public TimeSpan TotalProcessingTime => ProcessingEndTime - ProcessingStartTime;
        
        /// <summary>
        /// Gets or sets the detailed results for each processed policy.
        /// </summary>
        public List<PolicyProcessingResult> Results { get; set; } = new List<PolicyProcessingResult>();
        
        /// <summary>
        /// Generates and displays a comprehensive report of the processing results to the console.
        /// </summary>
        public void GenerateReport()
        {
            Console.WriteLine("=== POLICY PROCESSING SUMMARY ===");
            Console.WriteLine($"Processing Period: {ProcessingStartTime:yyyy-MM-dd HH:mm:ss} to {ProcessingEndTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Total Processing Time: {TotalProcessingTime:hh\\:mm\\:ss}");
            Console.WriteLine();
            
            Console.WriteLine("=== OVERALL STATISTICS ===");
            Console.WriteLine($"Total Policies Processed: {TotalPoliciesProcessed}");
            Console.WriteLine($"Successful Policies: {SuccessfulPolicies}");
            Console.WriteLine($"Failed Policies: {FailedPolicies}");
            Console.WriteLine($"Success Rate: {(TotalPoliciesProcessed > 0 ? (double)SuccessfulPolicies / TotalPoliciesProcessed * 100 : 0):F1}%");
            Console.WriteLine();
            
            Console.WriteLine($"Total XML Actions Attempted: {TotalXmlActionsAttempted}");
            Console.WriteLine($"Successful XML Actions: {SuccessfulXmlActions}");
            Console.WriteLine($"Failed XML Actions: {FailedXmlActions}");
            Console.WriteLine($"XML Action Success Rate: {(TotalXmlActionsAttempted > 0 ? (double)SuccessfulXmlActions / TotalXmlActionsAttempted * 100 : 0):F1}%");
            Console.WriteLine();
            
            var failedPolicies = Results.Where(r => !r.AllActionsSuccessful).ToList();
            if (failedPolicies.Any())
            {
                Console.WriteLine("=== FAILED POLICIES ===");
                foreach (var result in failedPolicies)
                {
                    Console.WriteLine($"Policy: {result.Symbol}{result.PolicyNumber}-{result.Module} (ID: {result.PolicyId})");
                    
                    if (!result.DeleteRequestResult.IsSuccessful)
                        Console.WriteLine($"  - Delete Request FAILED: {result.DeleteRequestResult.ErrorMessage}");
                    
                    if (!result.SetRNTReasonResult.IsSuccessful)
                        Console.WriteLine($"  - Set RNT Reason FAILED: {result.SetRNTReasonResult.ErrorMessage}");
                    
                    if (!result.CreateRNTActionResult.IsSuccessful)
                        Console.WriteLine($"  - Create RNT Action FAILED: {result.CreateRNTActionResult.ErrorMessage}");
                }
                Console.WriteLine();
            }
            
            var successfulPolicies = Results.Where(r => r.AllActionsSuccessful).ToList();
            if (successfulPolicies.Any())
            {
                Console.WriteLine("=== SUCCESSFUL POLICIES ===");
                foreach (var result in successfulPolicies)
                {
                    Console.WriteLine($"Policy: {result.Symbol}{result.PolicyNumber}-{result.Module} - ALL ACTIONS SUCCESSFUL");
                }
            }
        }
        
        /// <summary>
        /// Exports the detailed processing results to a CSV file for further analysis.
        /// </summary>
        /// <param name="filePath">The file path where the CSV should be saved</param>
        /// <returns>A task representing the asynchronous file write operation</returns>
        /// <exception cref="ArgumentException">Thrown when filePath is null or empty</exception>
        /// <exception cref="IOException">Thrown when file operations fail</exception>
        public async Task ExportToCsv(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                
            var csv = new StringBuilder();
            
            // CSV Header
            csv.AppendLine("PolicyId,Symbol,PolicyNumber,Module,Location,MasterCompany,ProcessingStartTime,ProcessingEndTime,AllActionsSuccessful," +
                          "DeleteRequest_Success,DeleteRequest_StatusCode,DeleteRequest_Error," +
                          "SetRNTReason_Success,SetRNTReason_StatusCode,SetRNTReason_Error," +
                          "CreateRNTAction_Success,CreateRNTAction_StatusCode,CreateRNTAction_Error");
            
            // Data rows
            foreach (var result in Results)
            {
                csv.AppendLine($"{EscapeCsv(result.PolicyId)},{EscapeCsv(result.Symbol)},{EscapeCsv(result.PolicyNumber)},{EscapeCsv(result.Module)}," +
                              $"{EscapeCsv(result.Location)},{EscapeCsv(result.MasterCompany)}," +
                              $"{result.ProcessingStartTime:yyyy-MM-dd HH:mm:ss},{result.ProcessingEndTime:yyyy-MM-dd HH:mm:ss}," +
                              $"{result.AllActionsSuccessful}," +
                              $"{result.DeleteRequestResult.IsSuccessful},{result.DeleteRequestResult.HttpStatusCode},{EscapeCsv(result.DeleteRequestResult.ErrorMessage)}," +
                              $"{result.SetRNTReasonResult.IsSuccessful},{result.SetRNTReasonResult.HttpStatusCode},{EscapeCsv(result.SetRNTReasonResult.ErrorMessage)}," +
                              $"{result.CreateRNTActionResult.IsSuccessful},{result.CreateRNTActionResult.HttpStatusCode},{EscapeCsv(result.CreateRNTActionResult.ErrorMessage)}");
            }
            
            await File.WriteAllTextAsync(filePath, csv.ToString());
            Console.WriteLine($"Detailed results exported to: {filePath}");
        }
        
        /// <summary>
        /// Escapes special characters in CSV field values to ensure proper formatting.
        /// </summary>
        /// <param name="field">The field value to escape</param>
        /// <returns>Properly escaped CSV field value</returns>
        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
                
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            
            return field;
        }
    }
}