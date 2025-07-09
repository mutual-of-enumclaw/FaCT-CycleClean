using System.Xml.Serialization;

namespace Fact.BatchCleaner
{
    public class PointXml
    {
        [XmlElement("SignonRq")]
        public SignonRequest SignonRq { get; set; } = new SignonRequest();

        [XmlElement("InsuranceSvcRq")]
        public InsuranceServiceRequest InsuranceSvcRq { get; set; } = new InsuranceServiceRequest();
    }

    public class SignonRequest
    {
        [XmlElement("ClientApp")]
        public ClientApp ClientApp { get; set; } = new ClientApp();

        [XmlElement("SignonPswd")]
        public SignonPassword SignonPswd { get; set; } = new SignonPassword();
    }

    public class ClientApp
    {
        [XmlElement("Name")]
        public string Name { get; set; } = string.Empty;
    }

    public class SignonPassword
    {
        [XmlElement("CustId")]
        public CustomerId CustId { get; set; } = new CustomerId();
    }

    public class CustomerId
    {
        [XmlElement("CustLoginId")]
        public string CustLoginId { get; set; } = string.Empty;
    }

    public class InsuranceServiceRequest
    {
        [XmlElement("RqUID")]
        public string RqUID { get; set; } = string.Empty;

        [XmlElement("CPPBCONTRq")]
        public CPPBCONTRq CPPBCONTRq { get; set; } = new CPPBCONTRq();
    }

    public class CPPBCONTRq
    {
        [XmlElement("PayLoad")]
        public PayLoad PayLoad { get; set; } = new PayLoad();
    }

    public class PayLoad
    {
        [XmlElement("BC_KEY_POLICY_NUMBER")]
        public string PolicyNumber { get; set; } = string.Empty;

        [XmlElement("BC_LATEST_POLICY_NUMBER")]
        public string LatestPolicyNumber { get; set; } = string.Empty;

        [XmlElement("BC_STATUS")]
        public string Status { get; set; } = string.Empty;

        [XmlElement("BC_RENEWAL_PAY_BY_PLAN")]
        public string RenewalPayByPlan { get; set; } = string.Empty;
    }
}
