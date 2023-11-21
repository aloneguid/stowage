using System;
using System.Collections.Generic;
using System.Text;

namespace Stowage.Impl.Amazon {

    /// <summary>
    /// This class is selectively copied from AWS SDK for .NET due to some non-conformant AWS specific URL encoding logic.
    /// </summary>
    internal static class AWSSDKUtils {
        internal static Dictionary<int, string> RFCEncodingSchemes = new Dictionary<int, string> {
            { 3986,  ValidUrlCharacters },
            { 1738,  ValidUrlCharactersRFC1738 }
        };

        /// <summary>
        /// The Set of accepted and valid Url characters per RFC3986. 
        /// Characters outside of this set will be encoded.
        /// </summary>
        private const string ValidUrlCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        /// <summary>
        /// The Set of accepted and valid Url characters per RFC1738. 
        /// Characters outside of this set will be encoded.
        /// </summary>
        public const string ValidUrlCharactersRFC1738 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.";

        // Checks which path characters should not be encoded
        // This set will be different for .NET 4 and .NET 4.5, as
        // per http://msdn.microsoft.com/en-us/library/hh367887%28v=vs.110%29.aspx
        private static string DetermineValidPathCharacters() {
            const string basePathCharacters = "/:'()!*[]$";

            var sb = new StringBuilder();
            foreach(char c in basePathCharacters) {
                string escaped = Uri.EscapeUriString(c.ToString());
                if(escaped.Length == 1 && escaped[0] == c)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// The set of accepted and valid Url path characters per RFC3986.
        /// </summary>
        private static readonly string ValidPathCharacters = DetermineValidPathCharacters();

        private static char ToUpperHex(int value) {
            // Maps 0-9 to the Unicode range of '0' - '9' (0x30 - 0x39).
            if(value <= 9) {
                return (char)(value + '0');
            }
            // Maps 10-15 to the Unicode range of 'A' - 'F' (0x41 - 0x46).
            return (char)(value - 10 + 'A');
        }

        /// <summary>
        /// This is a special version of URI Encoder used in AWS. It does not conform to standard rules, hence reimplementation below.
        /// See: https://github.com/aws/aws-sdk-net/blob/3981645e961ba35cb7d3d0fb6513ca2411811a74/sdk/src/Core/Amazon.Util/AWSSDKUtils.cs#L1098
        /// URL encodes a string per the specified RFC. If the path property is specified,
        /// the accepted path characters {/+:} are not encoded.
        /// </summary>
        /// <param name="rfcNumber">RFC number determing safe characters</param>
        /// <param name="data">The string to encode</param>
        /// <param name="path">Whether the string is a URL path or not</param>
        /// <returns>The encoded string</returns>
        /// <remarks>
        /// Currently recognised RFC versions are 1738 (Dec '94) and 3986 (Jan '05). 
        /// If the specified RFC is not recognised, 3986 is used by default.
        /// </remarks>
        public static string UrlEncode(int rfcNumber, string data, bool path) {
            StringBuilder encoded = new StringBuilder(data.Length * 2);
            string validUrlCharacters;
            if(!RFCEncodingSchemes.TryGetValue(rfcNumber, out validUrlCharacters))
                validUrlCharacters = ValidUrlCharacters;

            string unreservedChars = String.Concat(validUrlCharacters, (path ? ValidPathCharacters : ""));
            foreach(char symbol in System.Text.Encoding.UTF8.GetBytes(data)) {
                if(unreservedChars.IndexOf(symbol) != -1) {
                    encoded.Append(symbol);
                } else {
                    encoded.Append('%');

                    // Break apart the byte into two four-bit components and
                    // then convert each into their hexadecimal equivalent.
                    byte b = (byte)symbol;
                    int hiNibble = b >> 4;
                    int loNibble = b & 0xF;
                    encoded.Append(ToUpperHex(hiNibble));
                    encoded.Append(ToUpperHex(loNibble));
                }
            }

            return encoded.ToString();
        }

        /// <summary>
        /// URL encodes a string per RFC3986. If the path property is specified,
        /// the accepted path characters {/+:} are not encoded.
        /// </summary>
        /// <param name="data">The string to encode</param>
        /// <param name="path">Whether the string is a URL path or not</param>
        /// <returns>The encoded string</returns>
        public static string UrlEncode(string data, bool path) {
            return UrlEncode(3986, data, path);
        }
    }
}
