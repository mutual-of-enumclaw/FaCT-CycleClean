using System.Data;
using System.Xml.Linq;

namespace Fact.BatchCleaner
{
    public class XmlMessageBuilder : IXmlMessageBuilder
    {
        public XElement BuildSignOnRequest()
        {
            return new XElement("POINTXML",
                new XElement("SignonRq",
                    new XElement("ClientApp",
                        new XElement("Name", "PT")
                    ),
                    new XElement("SignonPswd",
                        new XElement("CustId",
                            new XElement("CustLoginId", "DLTACTION")
                        )
                    )
                )
            );
        }
        public XElement BuildDeleteRequest(DataRow row)
        {
            return new XElement("BCDeleteSvcRq",
                new XElement("RqUID", "CPPBCONTDLTRq"),
                new XElement("CPPBCONTRq",
                    new XElement("PayLoad",
                        new XElement("BC_KEY_POLICY_NUMBER", row["POLICY0NUM"] ?? ""),
                        new XElement("BC_LATEST_POLICY_NUMBER", row["POLICY0NUM"] ?? ""),
                        new XElement("BC_STATUS", "Pending"),
                        new XElement("BC_RENEWAL_PAY_BY_PLAN", "DA"),
                        new XElement("BC_KEY_MASTER_COMPANY", row["MASTER0CO"] ?? ""),
                        new XElement("BC_LATEST_POLICY_SYMBOL", row["SYMBOL"] ?? ""),
                        new XElement("BC_POLICY_EFFECTIVE_DATE", row["EFF_DATE1"] ?? ""),
                        new XElement("BC_POLICY_COMPANY", row["COMPANY0NO"] ?? ""),
                        new XElement("BC_LINE_OF_BUSINESS", row["LINE0BUS"] ?? ""),
                        new XElement("BC_KEY_LOCATION_COMPANY", row["LOCATION"] ?? ""),
                        new XElement("REQUESTCODE", "CPPBCONTDLTRq")
                    )
                )
            );
        }

        public XElement SetRNTReason(DataRow resultRow)
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

        public XElement CreateRNTActionRequest(DataRow resultRow)
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
}
