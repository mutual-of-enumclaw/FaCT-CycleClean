using System.Data;
using System.Xml.Linq;

namespace Fact.BatchCleaner
{
    public interface IXmlMessageBuilder
    {
        XElement BuildDeleteRequest(DataRow row);
        XElement SetRNTReason(DataRow resultRow);
        XElement CreateRNTActionRequest(DataRow resultRow);
    }
}
