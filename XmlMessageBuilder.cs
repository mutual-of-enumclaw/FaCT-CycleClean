using System.Data;
using System.Xml.Linq;

namespace Fact.BatchCleaner
{
    /// <summary>
    /// Builds XML messages for communication with the POINT system.
    /// This class creates the required XML structures for sign-on requests and various policy operations.
    /// </summary>
    public class XmlMessageBuilder : IXmlMessageBuilder
    {
        /// <summary>
        /// Builds a sign-on request XML element for authenticating with the POINT system.
        /// </summary>
        /// <returns>XML element containing sign-on request structure</returns>
        public XElement BuildSignOnRequest()
        {
            return new XElement("SignonRq",
                new XElement("ClientApp",
                    new XElement("Name", "PT")
                ),
                new XElement("SignonPswd",
                    new XElement("CustId",
                        new XElement("CustLoginId", "DltAction")
                    )
                )
            );
        }

        /// <summary>
        /// Builds a delete request XML element for removing a policy from the system.
        /// </summary>
        /// <param name="row">DataRow containing policy information from the database</param>
        /// <returns>XML element containing delete request structure</returns>
        /// <exception cref="InvalidOperationException">Thrown when required policy number is missing</exception>
        public XElement BuildDeleteRequest(DataRow row)
        {
            ArgumentNullException.ThrowIfNull(row);
            
            string Get(string col) => row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? row[col]?.ToString() ?? "" : "";

            var policyNumber = Get("POLICY0NUM");
            if (string.IsNullOrEmpty(policyNumber))
                throw new InvalidOperationException("POLICY0NUM is required for delete request.");
                
            return new XElement("InsuranceSvcRq",
                new XElement("RqUID", "CPPBCONTDLTRq"),
                new XElement("CPPBCONTRq",
                    new XElement("PayLoad",
                        // Risk and business information
                        new XElement("BC_RISK_UNDERWRITER", "$"),
                        new XElement("BC_PROFIT_CENTER", "000"),
                        new XElement("PROC_LDA_RENEWAL_MODULE", ""),
                        new XElement("BC_CHECK_NUMBER", ""),
                        new XElement("BC_BRANCH", "00"),
                        new XElement("BC_RISK_STATE", Get("RISK0STATE")),
                        new XElement("BC_AGENT_NAME", Get("ACNM_NAME1")),
                        
                        // Policy identification
                        new XElement("BC_KEY_POLICY_NUMBER", policyNumber),
                        new XElement("BC_LATEST_POLICY_NUMBER", policyNumber),
                        new XElement("BC_RENEWAL_POLICY_NUMBER", policyNumber),
                        new XElement("BC_KEY_MASTER_COMPANY", Get("MASTER0CO")),
                        new XElement("BC_KEY_SYMBOL", Get("SYMBOL")),
                        new XElement("BC_LATEST_POLICY_SYMBOL", Get("SYMBOL")),
                        new XElement("BC_RENEWAL_SYMBOL", Get("SYMBOL")),
                        new XElement("BC_KEY_MODULE", Get("MODULE")),
                        new XElement("BC_LATEST_POLICY_MODULE", Get("MODULE")),
                        new XElement("BC_KEY_LOCATION_COMPANY", Get("LOCATION")),
                        new XElement("BC_POLICY_COMPANY", Get("COMPANY0NO")),
                        
                        // Non-renewal reasons (empty for delete requests)
                        new XElement("PIF_NON_RENEWAL_RSN3", ""),
                        new XElement("PIF_NON_RENEWAL_RSN2", ""),
                        new XElement("PIF_NON_RENEWAL_RSN1", ""),
                        
                        // System and processing fields
                        new XElement("PROC_LDA_SECURITY", ""),
                        new XElement("fullkey", Get("POL_ID")),
                        new XElement("BC_STATUS", "Pending"),
                        new XElement("REQUESTCODE", "CPPBCONTDLTRq"),
                        new XElement("CurrentPageName", "POINT_Application_Information_N_B_N_A_Off"),
                        new XElement("BC_NUMBER_OF_INSTALLMENTS", ""),
                        new XElement("BC_CASH_WITH_APPLICATION", "0.00"),
                        new XElement("TMPREQUESTCODE", "null"),
                        new XElement("BC_REVIEW_CODE", "$"),
                        
                        // Special use and description fields
                        new XElement("BC_SPECIAL_USE_A_DESC", ""),
                        new XElement("BC_SPECIAL_USE_B_DESC", ""),
                        new XElement("BC_SPECIAL_USE_C_DESC", ""),
                        new XElement("BC_SPECIAL_USE_A", "Y"),
                        new XElement("BC_SPECIAL_USE_B", "N"),
                        new XElement("BC_SPECIAL_USE_C", "    "),
                        new XElement("BC_RENEW_PAYBY_DESC", ""),
                        
                        // Financial and premium information
                        new XElement("BC_AGENT_TOTAL_PREMIUM", "0.00"),
                        new XElement("BC_RENEWAL_PAY_BY_PLAN", "DA"),
                        new XElement("BC_PAY_BY_PLAN", "DA"),
                        new XElement("BC_PAYBY_MODE_DESC", ""),
                        new XElement("BC_INSTALLMENT_TERM", "012"),
                        
                        // System processing fields
                        new XElement("PROC_DEFAULT_LA_URL", ""),
                        new XElement("BC_VARIATION", "Y"),
                        new XElement("BC_REASON_AMEND_DIGIT3", ""),
                        new XElement("BC_REASON_AMEND_DIGIT2", ""),
                        new XElement("BC_REASON_AMEND_DIGIT1", ""),
                        new XElement("BC_REASON_AMEND_DESC", ""),
                        new XElement("BC_REASON_AMENDED", ""),
                        new XElement("NASWITCH", ""),
                        new XElement("BC_AMENDMENT_NUMBER", ""),
                        
                        // Address information
                        new XElement("BC_INSURED_ADDRESS_CITY", Get("CITY")),
                        new XElement("BC_INSURED_ADDRESS_STATE", Get("STATE")),
                        new XElement("BC_INSURED_ADDRESS_LINE_01", Get("ADDRLN1")),
                        new XElement("BC_INSURED_ADDRESS_LINE_02", Get("ADDRLN2")),
                        new XElement("BC_INSURED_ADDRESS_LINE_03", Get("ADDRLN3")),
                        new XElement("BC_ZIP_CODE", Get("ZIPCODE")),
                        
                        // Policy classification and status
                        new XElement("BC_KIND_CODE", "D"),
                        new XElement("BC_RENEWAL_CODE", "0"),
                        new XElement("BC_ISSUE_CODE_DESC", "RN Comp Rated"),
                        new XElement("BC_ISSUE_CODE", "R"),
                        new XElement("BC_TYPE_ACTIVITY", "RB"),
                        new XElement("BC_LINE_OF_BUSINESS", Get("LINE0BUS")),
                        new XElement("BC_COMPANY_LINE", Get("LINE0BUS")),
                        
                        // Date fields
                        new XElement("BC_POLICY_EFFECTIVE_DATE", Get("EFF_DATE1")),
                        new XElement("BC_POLICY_EXPIRATION_DATE", Get("EXP_DATE1")),
                        new XElement("BC_ENTERED_DATE", Get("EFF_DATE1")),
                        new XElement("BC_POLICY_CANCELLATION_DATE", "00/00/00"),
                        new XElement("BC_ORIGINAL_INCEPTION_DATE", ""),
                        new XElement("PROC_EFFECTIVE_DATE", Get("EFF_DATE2")),
                        
                        // Agent and customer information
                        new XElement("BC_AGENT_NUMBER", Get("AGENT_CODE")),
                        new XElement("BC_CUSTOMER_NUMBER", ""),
                        new XElement("BC_PRODUCER", "00"),
                        new XElement("BC_SORT_NAME", ""),
                        
                        // System and technical fields
                        new XElement("PROC_LDA_ISSUE_CODE", "E"),
                        new XElement("PROC_LDA_TRANSSEQ", "0"),
                        new XElement("PROC_LDA_SET_TYPE_NUMBER", "0"),
                        new XElement("PROC_LDA_TYPE_ACTIVITY", ""),
                        new XElement("PROC_LDA_ACCTG_DATE", ""),
                        new XElement("PROC_LDA_RECORD_IND", ""),
                        new XElement("PROC_LDA_CUR_REC_SET_STATUS", ""),
                        new XElement("PROC_SECURITY_INDIC", ""),
                        new XElement("PROC_TRANSACTION_TYPE", "RB"),
                        new XElement("PROC_LDA_SYSTEM_DATE_OVERRIDE", ""),
                        new XElement("PROC_PANEL_MODE", ""),
                        
                        // UI and display fields
                        new XElement("Refresh", "Yes"),
                        new XElement("target", "jsp/POINT_Application_Information_N_B_N_A_Off.jsp"),
                        new XElement("ScreenNav", "null"),
                        new XElement("tmptarget", "null"),
                        new XElement("VisibilityControls", ""),
                        new XElement("PushButtonValue", ""),
                        new XElement("PushButtonParams", ""),
                        
                        // Additional fields
                        new XElement("BC_MVR_REPORT_YEAR", "$"),
                        new XElement("BC_ADDRESS_SEQUENCE_NUMBER", "0"),
                        new XElement("BC_CLIENT_SEQUENCE_NUMBER", "0"),
                        new XElement("BC_KEY_TRANSACTION_STATUS", ""),
                        new XElement("BC_AUDIT_CODE", "N"),
                        new XElement("BC_RISK_GRADE", "$"),
                        new XElement("BC_TEMP_SPECIAL_USE_C", ""),
                        new XElement("TEMPHASHDATA", ""),
                        new XElement("DB_BEAN_BASIC_CONTRACT", ""),
                        new XElement("PIF_RATING_APPLICATION", "A"),
                        new XElement("PRINTED_STATEMENT", ""),
                        new XElement("User", "DMARTENS")
                    )
                )
            );
        }

        /// <summary>
        /// Builds an XML request to set the RNT (Renewal/Non-renewal) reason for a policy.
        /// </summary>
        /// <param name="resultRow">DataRow containing policy information from the database</param>
        /// <returns>XML element containing set RNT reason request structure</returns>
        public XElement SetRNTReason(DataRow resultRow)
        {
            ArgumentNullException.ThrowIfNull(resultRow);
            
            string Get(string col) => resultRow.Table.Columns.Contains(col) && resultRow[col] != DBNull.Value ? resultRow[col]?.ToString() ?? "" : "";

            return new XElement("InsuranceSvcRq",
                new XElement("RqUID", "RNWMONCOADDRq"),
                new XElement("RNWMONCORq",
                    new XElement("PayLoad",
                        // Navigation and system fields
                        new XElement("target", "jsp/PolRenMonDiaryComments.jsp"),
                        new XElement("ScreenNav", "null"),
                        new XElement("tmptarget", "null"),
                        new XElement("CurrentPageName", "PolRenMonComments"),
                        new XElement("TMPREQUESTCODE", "null"),
                        new XElement("REQUESTCODE", "RNWMONCOADDRq"),
                        
                        // Comment and policy identification
                        new XElement("COMMENT_KEY_MASTER_COMPANY", Get("MASTER0CO")),
                        new XElement("COMMENT_KEY_LOCATION_COMPANY", Get("LOCATION")),
                        new XElement("COMMENT_KEY_SYMBOL", Get("SYMBOL")),
                        new XElement("COMMENT_KEY_POLICY_NUMBER", Get("POLICY0NUM")),
                        new XElement("COMMENT_KEY_MODULE", Get("PRIOR_MODULE")),
                        new XElement("COMMENT_KEY_SEQUENCE_NUMBER", ""),
                        new XElement("COMMENT_KEY_ID_03", ""),
                        new XElement("COMMENT_KEY_TRANSACTION_STATUS", "V"),
                        new XElement("COMMENT_KEY_REASON_SUSPENDED", "PC"),
                        new XElement("COMMENT_KEY_SUSPENSE_DATE", Get("EXP_DATE1")),
                        new XElement("fullkey", Get("POL_ID")),
                        
                        // Comment settings and indicators
                        new XElement("A_COMMENT_PRINT_INDICATOR", "Y"),
                        new XElement("A_COMMENT_PERSON_ENTERING", "DLTACTION"),
                        new XElement("A_COMMENT_ROOM_DESTINATION", "RNT"),
                        
                        // Comment messages
                        new XElement("A_COMMENT_MESSAGE_1", "Placed business elsewhere"),
                        new XElement("A_COMMENT_MESSAGE_2", "Set policy to nonrenew state"),
                        new XElement("A_COMMENT_MESSAGE_3", ""),
                        new XElement("A_COMMENT_MESSAGE_4", ""),
                        new XElement("A_COMMENT_MESSAGE_5", ""),
                        new XElement("A_COMMENT_MESSAGE_6", ""),
                        new XElement("A_COMMENT_MESSAGE_7", ""),
                        new XElement("A_COMMENT_MESSAGE_8", ""),
                        
                        // Business and policy fields
                        new XElement("BC_POLICY_COMPANY", Get("COMPANY0NO")),
                        new XElement("BC_LINE_OF_BUSINESS", Get("SYMBOL")),
                        new XElement("BC_EFFECTIVE_DATE", Get("EFF_DATE2")),
                        new XElement("BC_EXPIRATION_DATE", Get("EXP_DATE2")),
                        new XElement("BC_FUNCTION", "RN"),
                        new XElement("BC_TRAN_STATUS", ""),
                        new XElement("BC_STATE", ""),
                        new XElement("BC_AGENT_NUMBER", Get("AGENT_CODE")),
                        new XElement("BC_NONRENCODE", "\""),
                        new XElement("BC_REAAMD_1", " "),
                        new XElement("BC_REAAMD_2", "1"),
                        new XElement("BC_REAAMD_3", "1"),
                        
                        // Date fields
                        new XElement("EXPIRATION_DATE", Get("EXP_DATE3")),
                        
                        // Processing fields
                        new XElement("PROC_LDA_SET_TYPE_NUMBER", "0"),
                        new XElement("PROC_LDA_TRANSSEQ", "0"),
                        
                        // UI and status fields
                        new XElement("RENEW_TO_QUOTE", ""),
                        new XElement("PushButtonValue", ""),
                        new XElement("PushButtonParams", ""),
                        new XElement("RNT", ""),
                        new XElement("NONRENEWED", ""),
                        new XElement("RENEWED", ""),
                        new XElement("HOLDRENEW", ""),
                        new XElement("CANCELED", ""),
                        new XElement("NONACTIVITY", ""),
                        new XElement("SYMBOL", Get("SYMBOL")),
                        new XElement("INSURED", ""),
                        new XElement("LINE_OF_BUSINESS", ""),
                        new XElement("Refresh", "Yes"),
                        new XElement("VisibilityControls", ""),
                        new XElement("DB_BEAN_COMMENTS", ""),
                        new XElement("User", "DLTACTION")
                    )
                )
            );
        }

        /// <summary>
        /// Builds an XML request to create an RNT (Renewal Not Taken) action for a policy.
        /// </summary>
        /// <param name="resultRow">DataRow containing policy information from the database</param>
        /// <returns>XML element containing create RNT action request structure</returns>
        public XElement CreateRNTActionRequest(DataRow resultRow)
        {
            ArgumentNullException.ThrowIfNull(resultRow);

            string Get(string col) => resultRow.Table.Columns.Contains(col) && resultRow[col] != DBNull.Value ? resultRow[col]?.ToString() ?? "" : "";

            // Build the KEY by concatenating policy identification fields
            string keyValue = $"{Get("LOCATION")}{Get("MASTER0CO")}{Get("SYMBOL")}{Get("POLICY0NUM")}{Get("MODULE")}";

            return new XElement("InsuranceSvcRq",
                new XElement("RqUID", "RNWMONCODFTRq"),
                new XElement("RNWMONCORq",
                    new XElement("PayLoad",
                        // Policy identification and keys
                        new XElement("KEY", $"\"{keyValue}\""),
                        new XElement("Key", Get("PRIOR_POLID")),
                        
                        // User and system information
                        new XElement("User", "DLTACTION"),
                        new XElement("REQUESTCODE", "RNWMONCODFTRq"),
                        new XElement("PROC_TRANSACTION_TYPE", "RN"),
                        
                        // Policy dates
                        new XElement("PolEffDt", Get("EFF_DATE1")),
                        new XElement("PolExpDt", Get("EXP_DATE1")),
                        new XElement("PolEndDt", "0000000"),
                        
                        // Policy reason and action settings
                        new XElement("policyReasonBoxNONRENEW", "Underwriting Reason           "),
                        new XElement("policyReasonBox", "11"),
                        new XElement("policyReasonBoxRNT", "Placed business elsewhere     "),
                        new XElement("policyActionBox", "RN"),
                        new XElement("PolAction", "RN"),
                        
                        // Line of business and status
                        new XElement("LOB", Get("SYMBOL")),
                        new XElement("policyStatus", "null"),
                        new XElement("Payby", "null"),
                        new XElement("longname", "null"),
                        
                        // UI and navigation
                        new XElement("Refresh", "No"),
                        new XElement("target", "jsp/PolRenMonDiaryComments.jsp"),
                        new XElement("RRSearchURL", "")
                    )
                )
            );
        }
    }
}
