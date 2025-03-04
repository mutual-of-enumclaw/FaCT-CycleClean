using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoE.Commercial.Data;
using MoE.Commercial.Data.Db2;
using System.Data;
using System.Xml.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;



internal class Program
{
    private static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var db2Settings = context.Configuration.GetSection("Db2").Get<Db2Settings>();

                // Register dependencies here
                services.AddTransient<ICycleClean, CycleClean>();
                services.AddTransient<IDataProvider, Db2OdbcDataProvider>(s => new Db2OdbcDataProvider(db2Settings));
            })
            .Build();

        // Resolve and run your application
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        var app = services.GetRequiredService<ICycleClean>();
        app.Run();
    }
}

public interface ICycleClean
{
    void Run();
}

public class CycleClean : ICycleClean
{
    readonly IDataProvider _dataProvider;
    readonly ILogger<CycleClean> _logger;

    public CycleClean(IDataProvider dataProvider, ILogger<CycleClean> logger)
    {
        _dataProvider = dataProvider;
        _logger = logger;
    }

    public async void Run()
    {
        try
        {
            //Pull in records that are unprocessed renewals, over X amount of time old
            //We are assuming these are abandoned and we can set them to be excluded
            //from the cycle.
            string cmd = DoSelect();

            var results = await _dataProvider.ExecuteSql(cmd);

            _logger.LogDebug($"Executing inline SELECT statement {cmd}");

            //For each policy found in the above statement, we will build and insert three messages.
            //These will:
            //   1) Delete the RB encountered
            //   2) Add a Renewal Not Taken comment to the policy
            //   3) Set the underlying policy to RNT
            //These changes will prevent the policy from trying to offer a new renewal term but
            //leaves the policy in a state that can be used, if there is a need. 
            if (results != null)
            {
                foreach (DataRow resultRow in results.Rows)
                {
                    Console.WriteLine(resultRow);

                    // Build <DoDeleteSvcRq> element
                    XElement DoDeleteSvcRq = BuildDeleteRequest(resultRow);

                    await SendRequestToCommFramework(DoDeleteSvcRq);

                    // Build <SetRNTReasonSvcRq> element
                    XElement SetRNTReasonSvcRq = SetRNTReason(resultRow);

                    await SendRequestToCommFramework(SetRNTReasonSvcRq);

                    // Build <DoRNTActionSvcRq> element
                    XElement DoRNTActionSvcRq = CreateRNTActionRequest(resultRow);

                    await SendRequestToCommFramework(DoRNTActionSvcRq);
                }
            }

            //_logger.LogDebug($"Executing inline DELETE statement {cmd}, deleting {deleteCount} rows");

            //await _dataProvider.ExecuteNonQuery(cmd, CommandType.Text, new GenericDbParameter[0]);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }

    private static readonly HttpClient _httpClient = new HttpClient();

    private async Task SendRequestToCommFramework(XElement request)
    {
        var requestUri = "http://point-tst1.mutualofenumclaw.net/cfwpi/servlet/CommFwServlet"; 
        var content = new StringContent(request.ToString(), Encoding.UTF8, "application/xml");

        try
        {
            var response = await _httpClient.PostAsync(requestUri, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Request to {requestUri} failed with status code {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {

            _logger.LogError(ex.ToString());
        }
    }

    private static string DoSelect()
    {
        var schema = "MOEDC0DAT";
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

    private static XElement BuildDeleteRequest(DataRow resultRow)
    {
        return new XElement("BCDeleteSvcRq",
                                new XElement("RqUID", "CPPBCONTDLTRq"),
                                new XElement("CPPBCONTRq",
                                    new XElement("PayLoad",
                                        new XElement("BC_RISK_UNDERWRITER", "$"),
                                        new XElement("BC_PROFIT_CENTER", "000"),
                                        new XElement("PROC_LDA_RENEWAL_MODULE"),
                                        new XElement("BC_CHECK_NUMBER"),
                                        new XElement("BC_BRANCH", "00"),
                                        new XElement("BC_RISK_STATE"),
                                        new XElement("BC_AGENT_NAME"),
                                        new XElement("BC_KEY_POLICY_NUMBER", resultRow["POLICY0NUM"] ?? ""),
                                        new XElement("PIF_NON_RENEWAL_RSN3"),
                                        new XElement("PIF_NON_RENEWAL_RSN2"),
                                        new XElement("BC_SPECIAL_USE_A_DESC"),
                                        new XElement("PIF_NON_RENEWAL_RSN1"),
                                        new XElement("PROC_LDA_SECURITY"),
                                        new XElement("fullkey"),
                                        new XElement("BC_KEY_MASTER_COMPANY", resultRow["MASTER0CO"] ?? ""),
                                        new XElement("BC_LATEST_POLICY_NUMBER", resultRow["POLICY0NUM"] ?? ""),
                                        new XElement("BC_STATUS", "Pending"),
                                        new XElement("REQUESTCODE", "CPPBCONTDLTRq"),
                                        new XElement("CurrentPageName", "POINT_Application_Information_N_B_N_A_Off"),
                                        new XElement("BC_NUMBER_OF_INSTALLMENTS"),
                                        new XElement("BC_CASH_WITH_APPLICATION", "0.00"),
                                        new XElement("TMPREQUESTCODE", "null"),
                                        new XElement("BC_REVIEW_CODE", "$"),
                                        new XElement("BC_LATEST_POLICY_MODULE", resultRow["MODULE"] ?? ""),
                                        new XElement("BC_SPECIAL_USE_B_DESC"),
                                        new XElement("BC_RENEW_PAYBY_DESC"),
                                        new XElement("BC_KEY_SYMBOL", resultRow["SYMBOL"] ?? ""),
                                        new XElement("BC_AGENT_TOTAL_PREMIUM"),
                                        new XElement("PROC_DEFAULT_LA_URL"),
                                        new XElement("BC_VARIATION", "Y"),
                                        new XElement("BC_REASON_AMEND_DIGIT3"),
                                        new XElement("BC_REASON_AMEND_DIGIT2"),
                                        new XElement("BC_REASON_AMEND_DIGIT1"),
                                        new XElement("NASWITCH"),
                                        new XElement("BC_AMENDMENT_NUMBER"),
                                        new XElement("BC_INSURED_ADDRESS_CITY"),
                                        new XElement("BC_KIND_CODE", "D"),
                                        new XElement("BC_RENEWAL_CODE", "0"),
                                        new XElement("BC_ISSUE_CODE_DESC", "RN Comp Rated"),
                                        new XElement("BC_ZIP_CODE"),
                                        new XElement("PROC_LDA_ISSUE_CODE", "E"),
                                        new XElement("BC_CUSTOMER_NUMBER"),
                                        new XElement("BC_SPECIAL_USE_C_DESC"),
                                        new XElement("BC_RENEWAL_PAY_BY_PLAN", "DA"),
                                        new XElement("Refresh", "Yes"),
                                        new XElement("PROC_LDA_SYSTEM_DATE_OVERRIDE"),
                                        new XElement("PROC_PANEL_MODE"),
                                        new XElement("BC_AGENT_NUMBER"),
                                        new XElement("BC_MVR_REPORT_YEAR", "$"),
                                        new XElement("PROC_LDA_TRANSSEQ", "0"),
                                        new XElement("BC_INSURED_ADDRESS_STATE"),
                                        new XElement("BC_POLICY_CANCELLATION_DATE", "00/00/00"),
                                        new XElement("PROC_LDA_TYPE_ACTIVITY"),
                                        new XElement("target", "jsp/POINT_Application_Information_N_B_N_A_Off.jsp"),
                                        new XElement("BC_POLICY_EXPIRATION_DATE"),
                                        new XElement("BC_SPECIAL_USE_C", "    "),
                                        new XElement("ScreenNav", "null"),
                                        new XElement("BC_SPECIAL_USE_B", "N"),
                                        new XElement("BC_SPECIAL_USE_A", "Y"),
                                        new XElement("BC_LATEST_POLICY_SYMBOL", resultRow["SYMBOL"] ?? ""),
                                        new XElement("BC_TYPE_ACTIVITY", "RB"),
                                        new XElement("User", "DMARTENS"),
                                        new XElement("PROC_LDA_CUR_REC_SET_STATUS"),
                                        new XElement("BC_COMPANY_LINE", "P"),
                                        new XElement("BC_ENTERED_DATE", resultRow["EFF_DATE1"] ?? ""),
                                        new XElement("BC_ISSUE_CODE", "R"),
                                        new XElement("BC_ADDRESS_SEQUENCE_NUMBER", "0"),
                                        new XElement("PROC_LDA_ACCTG_DATE"),
                                        new XElement("BC_INSURED_ADDRESS_LINE_03"),
                                        new XElement("BC_INSURED_ADDRESS_LINE_02"),
                                        new XElement("BC_INSURED_ADDRESS_LINE_01"),
                                        new XElement("BC_PAYBY_MODE_DESC", "Direct Bill     Annual"),
                                        new XElement("TEMPHASHDATA"),
                                        new XElement("BC_INSTALLMENT_TERM", "012"),
                                        new XElement("PROC_LDA_RECORD_IND"),
                                        new XElement("PROC_LDA_SET_TYPE_NUMBER", "0"),
                                        new XElement("BC_KEY_LOCATION_COMPANY", resultRow["LOCATION"] ?? ""),
                                        new XElement("VisibilityControls"),
                                        new XElement("BC_PRODUCER", "00"),
                                        new XElement("PushButtonValue"),
                                        new XElement("BC_PAY_BY_PLAN", "DA"),
                                        new XElement("PROC_EFFECTIVE_DATE", resultRow["EFF_DATE2"] ?? ""),
                                        new XElement("BC_ORIGINAL_INCEPTION_DATE"),
                                        new XElement("PIF_RATING_APPLICATION", "A"),
                                        new XElement("BC_POLICY_COMPANY", resultRow["COMPANY0NO"] ?? ""),
                                        new XElement("PushButtonParams"),
                                        new XElement("BC_REASON_AMENDED"),
                                        new XElement("tmptarget", "null"),
                                        new XElement("BC_CLIENT_SEQUENCE_NUMBER", "0"),
                                        new XElement("DB_BEAN_BASIC_CONTRACT"),
                                        new XElement("BC_AUDIT_CODE", "N"),
                                        new XElement("BC_REASON_AMEND_DESC"),
                                        new XElement("BC_RISK_GRADE", "$"),
                                        new XElement("BC_TEMP_SPECIAL_USE_C"),
                                        new XElement("BC_KEY_MODULE", "02"),
                                        new XElement("BC_RENEWAL_SYMBOL", resultRow["SYMBOL"] ?? ""),
                                        new XElement("BC_POLICY_EFFECTIVE_DATE", resultRow["EFF_DATE1"] ?? ""),
                                        new XElement("BC_LINE_OF_BUSINESS", resultRow["LINE0BUS"] ?? ""),
                                        new XElement("PROC_TRANSACTION_TYPE", "RB"),
                                        new XElement("BC_KEY_TRANSACTION_STATUS"),
                                        new XElement("BC_RENEWAL_POLICY_NUMBER", resultRow["POLICY0NUM"] ?? ""),
                                        new XElement("BC_SORT_NAME"),
                                        new XElement("PROC_SECURITY_INDIC"),
                                        new XElement("PRINTED_STATEMENT")
                                    )
                                )
                            );
    }

    private static XElement SetRNTReason(DataRow resultRow)
    {
        return new XElement("InsuranceSvcRq",
                    new XElement("RqUID", "RNWMONCOADDRq"),
                    new XElement("RNWMONCORq",
                        new XElement("PayLoad",
                            new XElement("target", "jsp/PolRenMonDiaryComments.jsp"),
                            new XElement("COMMENT_KEY_MASTER_COMPANY", resultRow["MASTER0CO"] ?? ""),
                            new XElement("A_COMMENT_PRINT_INDICATOR", "Y"),
                            new XElement("ScreenNav", "null"),
                            new XElement("TMPREQUESTCODE", "null"),
                            new XElement("RENEW_TO_QUOTE"),
                            new XElement("PushButtonValue"),
                            new XElement("COMMENT_KEY_ID_03"),
                            new XElement("BC_POLICY_COMPANY", resultRow["COMPANY0NO"] ?? ""),
                            new XElement("PROC_LDA_SET_TYPE_NUMBER", "0"),
                            new XElement("RNT"),
                            new XElement("CurrentPageName", "PolRenMonComments"),
                            new XElement("BC_TRAN_STATUS"),
                            new XElement("BC_EFFECTIVE_DATE", resultRow["EFF_DATE2"] ?? ""),
                            new XElement("tmptarget", "null"),
                            new XElement("BC_FUNCTION", "RN"),
                            new XElement("BC_LINE_OF_BUSINESS", resultRow["SYMBOL"] ?? ""),
                            new XElement("COMMENT_KEY_POLICY_NUMBER", resultRow["POLICY0NUM"] ?? ""),
                            new XElement("COMMENT_KEY_MODULE", resultRow["PRIOR_MODULE"] ?? ""), 
                            new XElement("EXPIRATION_DATE", resultRow["EXP_DATE3"] ?? ""),
                            new XElement("COMMENT_KEY_SEQUENCE_NUMBER"),
                            new XElement("BC_EXPIRATION_DATE", resultRow["EXP_DATE2"] ?? ""),
                            new XElement("PROC_LDA_TRANSSEQ", "0"),
                            new XElement("COMMENT_KEY_SUSPENSE_DATE", resultRow["EXP_DATE1"] ?? ""),
                            new XElement("A_COMMENT_PERSON_ENTERING", "DLTACTION"),
                            new XElement("User", "DLTACTION"),
                            new XElement("NONRENEWED"),
                            new XElement("BC_STATE"),
                            new XElement("RENEWED"),
                            new XElement("COMMENT_KEY_SYMBOL", resultRow["SYMBOL"] ?? ""),
                            new XElement("COMMENT_KEY_TRANSACTION_STATUS", "V"),
                            new XElement("COMMENT_KEY_REASON_SUSPENDED", "PC"),
                            new XElement("HOLDRENEW"),
                            new XElement("DB_BEAN_COMMENTS"),
                            new XElement("CANCELED"),
                            new XElement("BC_NONRENCODE", "\""),
                            new XElement("BC_REAAMD_3", "1"),
                            new XElement("A_COMMENT_ROOM_DESTINATION", "RNT"),
                            new XElement("BC_REAAMD_2", "1"),
                            new XElement("REQUESTCODE", "RNWMONCOADDRq"),
                            new XElement("BC_REAAMD_1", " "),
                            new XElement("fullkey", resultRow["POL_ID"] ?? ""),
                            new XElement("COMMENT_KEY_LOCATION_COMPANY", resultRow["LOCATION"] ?? ""),
                            new XElement("BC_AGENT_NUMBER", resultRow["AGENT_CODE"] ?? ""), 
                            new XElement("NONACTIVITY"),
                            new XElement("PushButtonParams"),
                            new XElement("SYMBOL", resultRow["SYMBOL"] ?? ""),
                            new XElement("INSURED"),
                            new XElement("Refresh", "Yes"),
                            new XElement("A_COMMENT_MESSAGE_8"),
                            new XElement("LINE_OF_BUSINESS"),
                            new XElement("A_COMMENT_MESSAGE_7"),
                            new XElement("A_COMMENT_MESSAGE_6"),
                            new XElement("A_COMMENT_MESSAGE_5"),
                            new XElement("A_COMMENT_MESSAGE_4"),
                            new XElement("VisibilityControls"),
                            new XElement("A_COMMENT_MESSAGE_3"),
                            new XElement("A_COMMENT_MESSAGE_2", "Set policy to nonrenew state"),
                            new XElement("A_COMMENT_MESSAGE_1", "Placed business elsewhere")
                        )
                    )
            );
    }

    private static XElement CreateRNTActionRequest(DataRow resultRow)
    {
        return new XElement("InsuranceSvcRq",
                    new XElement("RqUID", "RNWMONCODFTRq"),
                    new XElement("RNWMONCORq",
                        new XElement("PayLoad",
                            new XElement("KEY", "\"0001CPP000254201\""),
                            new XElement("User", "DLTACTION"),
                            new XElement("Key", resultRow["PRIOR_POLID"] ?? ""),
                            new XElement("PolEffDt", resultRow["EFF_DATE1"] ?? ""),
                            new XElement("policyReasonBoxNONRENEW", "Underwriting Reason           "),
                            new XElement("policyReasonBox", "11"),
                            new XElement("policyStatus", "null"),
                            new XElement("REQUESTCODE", "RNWMONCODFTRq"),
                            new XElement("PolEndDt", "0000000"),
                            new XElement("Payby", "null"),
                            new XElement("RRSearchURL"),
                            new XElement("LOB", resultRow["EFF_DATE1"] ?? ""),
                            new XElement("PolAction", "RN"),
                            new XElement("PolExpDt", resultRow["EXP_DATE1"] ?? ""),
                            new XElement("policyActionBox", "RN"),
                            new XElement("Refresh", "No"),
                            new XElement("longname", "null"),
                            new XElement("PROC_TRANSACTION_TYPE", "RN"),
                            new XElement("policyReasonBoxRNT", "Placed business elsewhere     "),
                            new XElement("target", "jsp/PolRenMonDiaryComments.jsp")
                        )
                    )
        );
    }
}