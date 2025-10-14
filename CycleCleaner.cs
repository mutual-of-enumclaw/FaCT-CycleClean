using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoE.Commercial.Data;
using MoE.Commercial.Data.Db2;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fact.BatchCleaner
{
    /// <summary>
    /// Main service responsible for processing insurance policies through a cycle cleaning operation.
    /// This service retrieves policies from a database, processes them through three XML actions
    /// (Delete, Set RNT Reason, Create RNT Action), and generates comprehensive reports.
    /// </summary>
    public class CycleCleaner : ICycleCleaner
    {
        private readonly HttpClient _httpClient;
        private readonly IDataProvider _dataProvider;
        private readonly ILogger<CycleCleaner> _logger;
        private readonly IXmlMessageBuilder _xmlMessageBuilder;
        private readonly string _requestUri;
        private readonly Db2Settings _db2Settings;
        private readonly LoggingSettings _loggingSettings;
        private readonly ProcessingSummary _processingSummary = new ProcessingSummary();

        /// <summary>
        /// Initializes a new instance of the CycleCleaner class.
        /// </summary>
        /// <param name="dataProvider">Database data provider for policy retrieval</param>
        /// <param name="logger">Logger for recording processing events</param>
        /// <param name="xmlMessageBuilder">Builder for creating XML messages</param>
        /// <param name="db2Settings">Database and communication framework settings</param>
        /// <param name="loggingSettings">Logging configuration settings</param>
        /// <param name="httpClient">HTTP client for sending XML requests</param>
        /// <exception cref="ArgumentNullException">Thrown when required configuration is missing</exception>
        public CycleCleaner(
            IDataProvider dataProvider,
            ILogger<CycleCleaner> logger,
            IXmlMessageBuilder xmlMessageBuilder,
            IOptions<Db2Settings> db2Settings,
            IOptions<LoggingSettings> loggingSettings,
            HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _xmlMessageBuilder = xmlMessageBuilder ?? throw new ArgumentNullException(nameof(xmlMessageBuilder));
            
            if (db2Settings?.Value == null)
                throw new ArgumentNullException(nameof(db2Settings));
            if (loggingSettings?.Value == null)
                throw new ArgumentNullException(nameof(loggingSettings));
                
            _db2Settings = db2Settings.Value;
            _loggingSettings = loggingSettings.Value;
            _requestUri = _db2Settings.CommFramework?.RequestUri
                          ?? throw new ArgumentNullException(nameof(db2Settings.Value.CommFramework.RequestUri), 
                             "CommFramework.RequestUri configuration is required");

            // Ensure MaxPolicyBatchSize has a reasonable default if not configured
            if (_db2Settings.MaxPolicyBatchSize == 0)
            {
                _logger.LogWarning("MaxPolicyBatchSize is set to 0 - this will process ALL matching policies (no limit)");
            }
            else if (_db2Settings.MaxPolicyBatchSize < 0)
            {
                _logger.LogWarning("MaxPolicyBatchSize is negative ({BatchSize}), treating as unlimited", _db2Settings.MaxPolicyBatchSize);
                _db2Settings.MaxPolicyBatchSize = 0; // Normalize negative values to 0
            }

            // Log HttpClient timeout for verification and troubleshooting
            _logger.LogInformation("CycleCleaner initialized with HttpClient timeout: {TimeoutSeconds} seconds ({TimeoutMinutes} minutes)", 
                _httpClient.Timeout.TotalSeconds, _httpClient.Timeout.TotalMinutes);
            
            // Log the batch size setting
            _logger.LogInformation("Policy batch size configured to: {BatchSize} {BatchSizeNote}", 
                _db2Settings.MaxPolicyBatchSize, 
                _db2Settings.MaxPolicyBatchSize <= 0 ? "(unlimited)" : "policies");
        }

        /// <summary>
        /// Executes the main policy cleaning process.
        /// Retrieves policies from the database and processes each through three XML actions.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task Run()
        {
            _processingSummary.ProcessingStartTime = DateTime.Now;
            _logger.LogInformation("Starting cycle cleaning process at {StartTime}", _processingSummary.ProcessingStartTime);
            
            try
            {
                string sql = BuildPolicyQuery();
                var results = await _dataProvider.ExecuteSql(sql);

                _logger.LogDebug("Executing SQL query: {SqlQuery}", sql);

                if (results?.Rows?.Count > 0)
                {
                    _logger.LogInformation("Retrieved {PolicyCount} policies for processing", results.Rows.Count);
                    await ProcessPolicies(results);
                }
                else
                {
                    _logger.LogWarning("No policies found matching the criteria");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error occurred during cycle cleaning process");
                throw;
            }
            finally
            {
                _processingSummary.ProcessingEndTime = DateTime.Now;
                await GenerateFinalReport();
            }
        }

        /// <summary>
        /// Processes each policy row through the three required XML actions.
        /// </summary>
        /// <param name="results">DataTable containing policy information</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task ProcessPolicies(DataTable results)
        {
            var semaphore = new SemaphoreSlim(2, 2); // Reduce from 5 to 2 concurrent operations to avoid overwhelming the service
            var tasks = new List<Task>();

            foreach (DataRow row in results.Rows)
            {
                tasks.Add(ProcessSinglePolicy(row, semaphore));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Processes a single policy through all three XML actions with proper concurrency control.
        /// </summary>
        /// <param name="row">DataRow containing policy information</param>
        /// <param name="semaphore">Semaphore to control concurrent operations</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task ProcessSinglePolicy(DataRow row, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            
            try
            {
                var policyResult = PolicyProcessingResult.FromDataRow(row);
                _processingSummary.Results.Add(policyResult);
                _processingSummary.TotalPoliciesProcessed++;

                _logger.LogInformation("Processing policy {PolicyId} ({PolicyNumber}-{Module})", 
                    policyResult.PolicyId, policyResult.PolicyNumber, policyResult.Module);

                // Process the three XML actions sequentially for each policy
                var deleteRequest = _xmlMessageBuilder.BuildDeleteRequest(row);
                policyResult.DeleteRequestResult = await SendPointXmlRequest(deleteRequest, "BuildDeleteRequest");

                var setReasonRequest = _xmlMessageBuilder.SetRNTReason(row);
                policyResult.SetRNTReasonResult = await SendPointXmlRequest(setReasonRequest, "SetRNTReason");

                var rntActionRequest = _xmlMessageBuilder.CreateRNTActionRequest(row);
                policyResult.CreateRNTActionResult = await SendPointXmlRequest(rntActionRequest, "CreateRNTActionRequest");

                policyResult.ProcessingEndTime = DateTime.Now;

                // Update summary statistics and log policy completion marker - this will ALWAYS appear in logs
                if (policyResult.AllActionsSuccessful)
                {
                    _processingSummary.SuccessfulPolicies++;
                    _logger.LogWarning("*** POLICY COMPLETED SUCCESSFULLY *** PolicyId: {PolicyId} ({PolicyNumber}-{Module}) - All actions passed", 
                        policyResult.PolicyId, policyResult.PolicyNumber, policyResult.Module);
                }
                else
                {
                    _processingSummary.FailedPolicies++;
                    var failedActions = new List<string>();
                    if (!policyResult.DeleteRequestResult.IsSuccessful) failedActions.Add("Delete");
                    if (!policyResult.SetRNTReasonResult.IsSuccessful) failedActions.Add("SetRNTReason");
                    if (!policyResult.CreateRNTActionResult.IsSuccessful) failedActions.Add("CreateRNTAction");
                    
                    _logger.LogWarning("*** POLICY COMPLETED WITH FAILURES *** PolicyId: {PolicyId} ({PolicyNumber}-{Module}) - Failed actions: {FailedActions}", 
                        policyResult.PolicyId, policyResult.PolicyNumber, policyResult.Module, string.Join(", ", failedActions));
                }
            }
            catch (Exception ex)
            {
                _processingSummary.FailedPolicies++;
                _logger.LogError(ex, "Error processing policy from row data");
                
                // Mark any unprocessed actions as failed for the policy result
                var policyResult = _processingSummary.Results.LastOrDefault();
                if (policyResult != null)
                {
                    MarkUnprocessedActionsAsFailed(policyResult, ex.Message);
                    // Log completion marker for exception case as well
                    _logger.LogWarning("*** POLICY COMPLETED WITH EXCEPTION *** PolicyId: {PolicyId} ({PolicyNumber}-{Module}) - Exception: {ErrorMessage}", 
                        policyResult.PolicyId, policyResult.PolicyNumber, policyResult.Module, ex.Message);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Marks any unprocessed XML actions as failed for a policy result.
        /// </summary>
        /// <param name="policyResult">The policy result to update</param>
        /// <param name="errorMessage">Error message to record</param>
        private static void MarkUnprocessedActionsAsFailed(PolicyProcessingResult policyResult, string errorMessage)
        {
            if (string.IsNullOrEmpty(policyResult.DeleteRequestResult.ActionName))
                policyResult.DeleteRequestResult = CreateFailedResult("BuildDeleteRequest", errorMessage);
            if (string.IsNullOrEmpty(policyResult.SetRNTReasonResult.ActionName))
                policyResult.SetRNTReasonResult = CreateFailedResult("SetRNTReason", errorMessage);
            if (string.IsNullOrEmpty(policyResult.CreateRNTActionResult.ActionName))
                policyResult.CreateRNTActionResult = CreateFailedResult("CreateRNTActionRequest", errorMessage);
        }

        /// <summary>
        /// Sends a single XML request to the communication framework and tracks the result.
        /// </summary>
        /// <param name="request">The XML element to send</param>
        /// <param name="requestType">Type of request for logging purposes</param>
        /// <returns>Result of the XML action</returns>
        private async Task<XmlActionResult> SendPointXmlRequest(XElement request, string requestType)
        {
            var startTime = DateTime.Now;
            var result = new XmlActionResult
            {
                ActionName = requestType,
                ActionTime = startTime
            };

            _processingSummary.TotalXmlActionsAttempted++;

            try
            {
                XElement signOnRequest = _xmlMessageBuilder.BuildSignOnRequest();
                var pointXml = new XElement("POINTXML", signOnRequest, request);

                // Only log detailed XML in verbose mode (Debug level)
                _logger.LogDebug("Sending {RequestType} request", requestType);
                _logger.LogTrace("SignOn XML: {SignOnXml}", signOnRequest.ToString(SaveOptions.None));
                _logger.LogTrace("Business Request XML: {BusinessXml}", request.ToString(SaveOptions.None));
                _logger.LogTrace("{RequestType} Final Combined XML: {CombinedXml}", requestType, pointXml.ToString(SaveOptions.None));
                
                var httpResult = await SendRequestToCommFramework(pointXml);
                
                result.IsSuccessful = httpResult.IsSuccessful;
                result.HttpStatusCode = httpResult.StatusCode;
                result.ResponseBody = httpResult.ResponseBody;
                result.ErrorMessage = httpResult.ErrorMessage;
                
                if (result.IsSuccessful)
                {
                    _processingSummary.SuccessfulXmlActions++;
                    _logger.LogDebug("{RequestType} completed successfully", requestType);
                }
                else
                {
                    _processingSummary.FailedXmlActions++;
                    _logger.LogWarning("{RequestType} failed: {ErrorMessage}", requestType, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _processingSummary.FailedXmlActions++;
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Exception in {RequestType}", requestType);
            }
            
            result.Duration = DateTime.Now - startTime;
            return result;
        }

        /// <summary>
        /// Creates a failed XML action result.
        /// </summary>
        /// <param name="actionName">Name of the action that failed</param>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Failed XmlActionResult</returns>
        private static XmlActionResult CreateFailedResult(string actionName, string errorMessage)
        {
            return new XmlActionResult
            {
                ActionName = actionName,
                IsSuccessful = false,
                ErrorMessage = errorMessage,
                ActionTime = DateTime.Now
            };
        }

        /// <summary>
        /// Generates and displays the final processing report and exports results to CSV.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task GenerateFinalReport()
        {
            _logger.LogInformation("Generating final processing report");
            
            _processingSummary.GenerateReport();
            
            // Export detailed results to CSV in the configured logs directory
            var csvFileName = $"PolicyProcessingResults_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var csvFilePath = Path.Combine(_loggingSettings.LogDirectory, csvFileName);
            
            try
            {
                // Ensure the logs directory exists (it should already exist from logging setup)
                if (!Directory.Exists(_loggingSettings.LogDirectory))
                {
                    Directory.CreateDirectory(_loggingSettings.LogDirectory);
                    _logger.LogInformation("Created logs directory: {LogsDirectory}", _loggingSettings.LogDirectory);
                }
                
                await _processingSummary.ExportToCsv(csvFilePath);
                _logger.LogWarning("*** CSV RESULTS EXPORTED *** Detailed results exported to: {CsvFilePath}", csvFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export CSV report to {CsvFilePath}", csvFilePath);
            }
            
            // Log summary for structured logging
            _logger.LogInformation("Processing Summary: {TotalPolicies} policies processed, {Successful} successful, {Failed} failed, Duration: {Duration}",
                _processingSummary.TotalPoliciesProcessed,
                _processingSummary.SuccessfulPolicies,
                _processingSummary.FailedPolicies,
                _processingSummary.TotalProcessingTime);
        }

        /// <summary>
        /// Builds the SQL query for retrieving policies that need to to be processed.
        /// The number of policies returned is controlled by the MaxPolicyBatchSize configuration setting.
        /// </summary>
        /// <returns>SQL query string</returns>
        private string BuildPolicyQuery()
        {
            var schema = _dataProvider.Schema ?? "SND1C0DAT";
            
            var baseQuery = $@"
                SELECT DISTINCT 
                    P.LOCATION,
                    P.MASTER0CO,
                    P.SYMBOL || P.POLICY0NUM || P.MODULE AS POL_ID,
                    P.SYMBOL,
                    P.POLICY0NUM,
                    P.MODULE,
                    P.SYMBOL || P.POLICY0NUM || RIGHT('00' || (CAST(P.MODULE AS NUM) - 1), 2) AS PRIOR_POLID,
                    RIGHT('00' || (CAST(P.MODULE AS NUM) - 1), 2) AS PRIOR_MODULE,
                    P.LINE0BUS,
                    P.COMPANY0NO,
                    P.RISK0STATE,
                    (SELECT STATECDKEY
                            FROM {schema}.TBTS01
                            WHERE STATEABBRV = RISK0STATE) AS STATECODE,
                    P.TYPE0ACT,
                    P.EFF0MO || '/' || P.EFF0DA || '/' || SUBSTR(P.EFF0YR, 2, 2) AS EFF_DATE1,
                    P.EFF0YR || P.EFF0MO || P.EFF0DA AS EFF_DATE2,
                    P.EXP0MO || '/' || P.EXP0DA || '/' || SUBSTR(P.EXP0YR, 2, 2) AS EXP_DATE1,
                    P.EXP0YR || P.EXP0MO || P.EXP0DA AS EXP_DATE2,
                    P.EXP0MO || (P.EXP0YR + 1900) AS EXP_DATE3,
                    P.FILLR1 || P.RPT0AGT0NR || P.FILLR2 AS AGENT_CODE,
                    (SELECT ACNM_NAME1
                            FROM {schema}.PMSPAG00
                            WHERE AGNM_AGCY = FILLR1 || RPT0AGT0NR || FILLR2) AS ACNM_NAME1,
                    ISSUE0CODE,
                    A3.ADDRLN1,
                    A3.ADDRLN2,
                    A3.ADDRLN3,
                    A3.CITY,
                    A3.STATE,
                    SUBSTR(A3.ZIPCODE, 1, 5) AS ZIPCODE
                FROM {schema}.PMSP0200 P
                        INNER JOIN {schema}.BASCLT1400 A1
                            ON A1.SYMBOL = P.SYMBOL
                                AND A1.POLICY0NUM = P.POLICY0NUM
                                AND A1.MODULE = P.MODULE
                        INNER JOIN {schema}.BASCLT0300 A3
                            ON A3.CLTSEQNUM = A1.CLTSEQNUM
                WHERE ISSUE0CODE <> 'Q'
                        AND TRANS0STAT = 'P'
                        AND EFF0YR < (SELECT SUBSTR(CYCLE_DT, 1, 3) - 1 AS DATE_CHECK
                                FROM {schema}.PMSPDATE
                                WHERE KEYFIELD = '')
                        AND P.SYMBOL || P.POLICY0NUM || P.MODULE NOT IN (
                            SELECT SYMBOL || POLICY0NUM || MODULE
                            FROM {schema}.PMSP0200
                            WHERE ISSUE0CODE <> 'Q'
                                AND TRANS0STAT = 'V')
                        AND P.MODULE > '00' 
                ORDER BY P.LOCATION,
                            P.MASTER0CO,
                            P.SYMBOL,
                            P.POLICY0NUM,
                            P.MODULE,
                            P.COMPANY0NO";

            // Add LIMIT clause only if MaxPolicyBatchSize is greater than 0
            if (_db2Settings.MaxPolicyBatchSize > 0)
            {
                baseQuery += $" LIMIT {_db2Settings.MaxPolicyBatchSize}";
                _logger.LogDebug("SQL query will be limited to {MaxPolicies} policies", _db2Settings.MaxPolicyBatchSize);
            }
            else
            {
                _logger.LogDebug("SQL query will return all matching policies (no limit applied)");
            }

            return baseQuery;
        }

        /// <summary>
        /// Sends an HTTP request to the communication framework with retry logic.
        /// </summary>
        /// <param name="request">XML element to send</param>
        /// <returns>HTTP result containing success/failure information</returns>
        private async Task<HttpResult> SendRequestToCommFramework(XElement request)
        {
            var xmlString = request.ToString();
            var maxRetries = 2; // Retry up to 2 times
            var baseDelay = TimeSpan.FromSeconds(30); // Wait 30 seconds between retries

            for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
            {
                _logger.LogTrace("HTTP request attempt {Attempt}/{MaxAttempts} to {RequestUri}", attempt, maxRetries + 1, _requestUri);
                _logger.LogTrace("HttpClient timeout: {TimeoutSeconds} seconds", _httpClient.Timeout.TotalSeconds);
                _logger.LogTrace("Raw XML being sent: {XmlContent}", xmlString);
                
                var content = new StringContent(xmlString, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", "");

                try
                {
                    var response = await _httpClient.PostAsync(_requestUri, content);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("HTTP request failed with status {StatusCode} on attempt {Attempt}: {ResponseBody}", 
                            response.StatusCode, attempt, responseBody);
                        _logger.LogTrace("Failed request XML was: {RequestXml}", xmlString);
                        
                        return new HttpResult
                        {
                            IsSuccessful = false,
                            StatusCode = (int)response.StatusCode,
                            ResponseBody = responseBody,
                            ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
                        };
                    }

                    _logger.LogTrace("HTTP request succeeded with status {StatusCode} on attempt {Attempt}", response.StatusCode, attempt);
                    _logger.LogTrace("Response body: {ResponseBody}", responseBody);
                    return new HttpResult
                    {
                        IsSuccessful = true,
                        StatusCode = (int)response.StatusCode,
                        ResponseBody = responseBody
                    };
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    _logger.LogWarning("HTTP request timed out after {TimeoutSeconds} seconds on attempt {Attempt}/{MaxAttempts}. HttpClient timeout was configured as {ConfiguredTimeout} seconds", 
                        _httpClient.Timeout.TotalSeconds, attempt, maxRetries + 1, _httpClient.Timeout.TotalSeconds);
                    
                    if (attempt <= maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(baseDelay.TotalSeconds * attempt); // Exponential backoff
                        _logger.LogInformation("Retrying request in {DelaySeconds} seconds...", delay.TotalSeconds);
                        await Task.Delay(delay);
                        continue;
                    }
                    
                    _logger.LogTrace("Timed out request XML was: {RequestXml}", xmlString);
                    return new HttpResult
                    {
                        IsSuccessful = false,
                        StatusCode = 0,
                        ErrorMessage = $"Request timed out after {_httpClient.Timeout.TotalSeconds} seconds (attempted {maxRetries + 1} times)",
                        ResponseBody = ""
                    };
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning("HTTP request was canceled (likely timeout) on attempt {Attempt}/{MaxAttempts}. HttpClient timeout: {TimeoutSeconds} seconds. Exception: {Exception}", 
                        attempt, maxRetries + 1, _httpClient.Timeout.TotalSeconds, ex.Message);
                    
                    if (attempt <= maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(baseDelay.TotalSeconds * attempt);
                        _logger.LogInformation("Retrying request in {DelaySeconds} seconds...", delay.TotalSeconds);
                        await Task.Delay(delay);
                        continue;
                    }
                    
                    _logger.LogTrace("Canceled request XML was: {RequestXml}", xmlString);
                    return new HttpResult
                    {
                        IsSuccessful = false,
                        StatusCode = 0,
                        ErrorMessage = $"Request was canceled (timeout after {_httpClient.Timeout.TotalSeconds} seconds): {ex.Message} (attempted {maxRetries + 1} times)",
                        ResponseBody = ""
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while sending HTTP request on attempt {Attempt}", attempt);
                    _logger.LogTrace("Failed request XML was: {RequestXml}", xmlString);
                    return new HttpResult
                    {
                        IsSuccessful = false,
                        StatusCode = 0,
                        ErrorMessage = ex.Message,
                        ResponseBody = ""
                    };
                }
            }

            // This should never be reached, but included for completeness
            return new HttpResult
            {
                IsSuccessful = false,
                StatusCode = 0,
                ErrorMessage = "Maximum retry attempts exceeded",
                ResponseBody = ""
            };
        }
    }

    /// <summary>
    /// Represents the result of an HTTP request to the communication framework.
    /// </summary>
    public class HttpResult
    {
        /// <summary>
        /// Gets or sets whether the HTTP request was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code returned.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the response body content.
        /// </summary>
        public string ResponseBody { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets any error message that occurred.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
