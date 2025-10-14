using System.Data;
using System.Xml.Linq;

namespace Fact.BatchCleaner
{
    /// <summary>
    /// Defines the contract for building XML messages used in communication with the POINT system.
    /// This interface provides methods for creating different types of XML requests required
    /// for policy processing operations.
    /// </summary>
    public interface IXmlMessageBuilder
    {
        /// <summary>
        /// Builds a sign-on request XML element for authenticating with the POINT system.
        /// </summary>
        /// <returns>XML element containing the sign-on request structure</returns>
        XElement BuildSignOnRequest();

        /// <summary>
        /// Builds a delete request XML element for removing a policy from the system.
        /// </summary>
        /// <param name="row">DataRow containing policy information from the database</param>
        /// <returns>XML element containing the delete request structure</returns>
        /// <exception cref="ArgumentNullException">Thrown when row is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when required policy data is missing</exception>
        XElement BuildDeleteRequest(DataRow row);

        /// <summary>
        /// Builds an XML request to set the RNT (Renewal/Non-renewal) reason for a policy.
        /// This operation adds comments and reasons for policy non-renewal.
        /// </summary>
        /// <param name="resultRow">DataRow containing policy information from the database</param>
        /// <returns>XML element containing the set RNT reason request structure</returns>
        /// <exception cref="ArgumentNullException">Thrown when resultRow is null</exception>
        XElement SetRNTReason(DataRow resultRow);

        /// <summary>
        /// Builds an XML request to create an RNT (Renewal/Non-renewal) action for a policy.
        /// This operation finalizes the non-renewal process for the policy.
        /// </summary>
        /// <param name="resultRow">DataRow containing policy information from the database</param>
        /// <returns>XML element containing the create RNT action request structure</returns>
        /// <exception cref="ArgumentNullException">Thrown when resultRow is null</exception>
        XElement CreateRNTActionRequest(DataRow resultRow);
    }
}
