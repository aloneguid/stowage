using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Text;
using System.Xml.Linq;

namespace Stowage.Impl.Microsoft {

    /// <summary>
    /// Azure specific error data
    /// </summary>
    public class AzureException : Exception {

        /// <summary>
        /// Constructs a new AzureException
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public AzureException(string code, string? message, Exception? inner) : base(message, inner) {
            Code = code;
        }

        /// <summary>
        /// Code
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Query parameter name
        /// </summary>
        public string? QueryParameterName { get; set; }

        /// <summary>
        /// Query parameter value
        /// </summary>
        public string? QueryParameterValue { get; set; }

        /// <summary>
        /// Reason
        /// </summary>
        public string? Reason { get; set; }

        /// <inheritdoc/>
        public override string ToString() => $"Code: {Code}. Message: {Message}. Parameter: {QueryParameterName}={QueryParameterValue}. Reason: {Reason}.";


        /*
         * sample:
<?xml version="1.0" encoding="UTF-8"?>
<Error>
    <Code>InvalidQueryParameterValue</Code>
    <Message>Value for one of the query parameters specified in the request URI is invalid. RequestId:616e04d5-701e-00c9-28af-26a61c000000 Time:2023-12-04T12:44:59.7660976Z</Message>
    <QueryParameterName>include</QueryParameterName>
    <QueryParameterValue>system</QueryParameterValue>
    <Reason>Invalid query parameter value.</Reason>
</Error>
         */

        /// <summary>
        /// Tries to instantiate the exception instance from the raw response XML
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="inner"></param>
        /// <param name="azureException"></param>
        /// <returns></returns>
        public static bool TryCreateFromXml(string xml, Exception? inner, out AzureException? azureException) {
            XElement x = XElement.Parse(xml);
            string? code = x.Element("Code")?.Value;
            if(code == null) {
                azureException = null;
                return false;
            }

            azureException = new AzureException(code, x.Element("Message")?.Value, inner) {
                QueryParameterName = x.Element("QueryParameterName")?.Value,
                QueryParameterValue = x.Element("QueryParameterValue")?.Value,
                Reason = x.Element("Reason")?.Value
            };
            return true;
        }
    }
}
