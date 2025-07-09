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
    public class CycleCleaner : ICycleCleaner
    {
        private readonly HttpClient _httpClient;
        private readonly IDataProvider _dataProvider;
        private readonly ILogger<CycleCleaner> _logger;
        private readonly IXmlMessageBuilder _xmlMessageBuilder;
        private readonly string _requestUri;

        private readonly Func<DataRow, XElement>[] _requestBuilders;

        public CycleCleaner(
            IDataProvider dataProvider,
            ILogger<CycleCleaner> logger,
            IXmlMessageBuilder xmlMessageBuilder,
            IOptions<Db2Settings> db2Settings,
            HttpClient httpClient)
        {
            _httpClient = httpClient;
            _dataProvider = dataProvider;
            _logger = logger;
            _xmlMessageBuilder = xmlMessageBuilder;
            _requestUri = db2Settings.Value.CommFramework?.RequestUri
                          ?? throw new ArgumentNullException("CommFramework.RequestUri");

            _requestBuilders = new Func<DataRow, XElement>[]
            {
                row => _xmlMessageBuilder.BuildDeleteRequest(row),
                row => _xmlMessageBuilder.SetRNTReason(row),
                row => _xmlMessageBuilder.CreateRNTActionRequest(row)
            };
        }

        public async Task Run()
        {
            try
            {
                string cmd = DoSelect();
                var results = await _dataProvider.ExecuteSql(cmd);

                _logger.LogDebug($"Executing SQL: {cmd}");

                if (results != null)
                {
                    await ProcessRows(results);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Run(): {ex}");
            }
        }

        private async Task ProcessRows(DataTable results)
        {
            foreach (DataRow row in results.Rows)
            {
                foreach (var builder in _requestBuilders)
                {
                    await SendPointXmlRequest(builder(row), builder.Method.Name);
                }
            }
        }

        private async Task SendPointXmlRequest(XElement request, string requestType)
        {
            // Cast to concrete type to access BuildSignOnRequest()
            XElement signOnRequest = ((XmlMessageBuilder)_xmlMessageBuilder).BuildSignOnRequest();
            var pointXml = new XElement("POINTXML", signOnRequest, request);

            _logger.LogDebug($"{requestType} XML: {pointXml.ToString(SaveOptions.None)}");
            await SendRequestToCommFramework(pointXml);
        }

        private string DoSelect()
        {
            var schema = _dataProvider.Schema ?? "SND1C0DAT";
            return $@"SELECT DISTINCT 
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
                            WHERE AGNM_AGCY = FILLR1 || RPT0AGT0NR || FILLR2),
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
                        AND EFF0YR < (SELECT SUBSTR(CYCLE_DT, 1, 3) - 2 AS DATE_CHECK
                                FROM {schema}.PMSPDATE
                                WHERE KEYFIELD = '')
                        AND P.SYMBOL || P.POLICY0NUM || P.MODULE NOT IN (SELECT SYMBOL || POLICY0NUM || MODULE
                                FROM {schema}.PMSP0200
                                WHERE ISSUE0CODE <> 'Q'
                                    AND TRANS0STAT = 'V')
                        AND P.MODULE > '00' 
                ORDER BY P.LOCATION,
                            P.MASTER0CO,
                            P.SYMBOL,
                            P.POLICY0NUM,
                            P.MODULE,
                            P.COMPANY0NO ";
        }

        private async Task SendRequestToCommFramework(XElement request)
        {
            var content = new StringContent(request.ToString(), Encoding.UTF8, "application/xml");

            try
            {
                var response = await _httpClient.PostAsync(_requestUri, content);

                if (!response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Request failed: {StatusCode}, Response: {ResponseBody}", response.StatusCode, responseBody);
                    throw new HttpRequestException($"Request failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP Error");
            }
        }
    }
}
