using System;
using System.Collections.Generic;
using System.Text;

namespace Stowage {
    /// <summary>
    /// Holds a parsed connection string to the storage.
    /// Originally designed to parse a string, but can be used to accept any key-value source like .ini files.
    /// </summary>
    public class ConnectionString {
        private const string PrefixSeparator = "://";
        private static readonly char[] PartsSeparators = [';'];
        private static readonly char[] PartSeparator = ['='];

        private readonly Dictionary<string, string> _parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a new instance of <see cref="ConnectionString"/>
        /// </summary>
        /// <param name="connectionString"></param>
        public ConnectionString(string connectionString) {
            Prefix = ParseAndGetPrefix(connectionString);
        }

        /// <summary>
        /// Creates a new instance of <see cref="ConnectionString"/> using key-value pairs
        /// </summary>
        /// <param name="parameters"></param>
        public ConnectionString(Dictionary<string, string> parameters) {
            _parts = parameters;
            if(!_parts.TryGetValue(KnownParameter.Prefix, out string? prefix)) {
                throw new ArgumentException($"'{KnownParameter.Prefix}' is required", nameof(parameters));
            }
            Prefix = prefix;
        }

        /// <summary>
        /// Gets or sets connection string parameters. Trying to fetch by null or non-existing key returns null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string? this[string? key] {
            get {
                if(key == null)
                    return null;
                _parts.TryGetValue(key, out string? value);
                return value;
            }
            set {
                if(key == null || value == null)
                    return;

                _parts[key] = value;
            }
        }

        /// <summary>
        /// Connection string parameters exposed as key-value pairs
        /// </summary>
        public Dictionary<string, string> Parameters => _parts;

        /// <summary>
        /// Prefix of this connection string, excluding prefix separator, i.e. for 'disk://something' the prefix is 'disk'
        /// </summary>
        public string Prefix { get; private set; }

        /// <summary>
        /// Gets the value of the parameter as when it's required. When parameter is not present, throws standard <see cref="ArgumentException"/>
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="requireNonEmptyValue">When true, checks that parameter value is not null or empty and throws <see cref="ArgumentException"/></param>
        /// <param name="value">Result value</param>
        public void GetRequired(string parameterName, bool requireNonEmptyValue, out string value) {
            if(parameterName == null) {
                throw new ArgumentNullException(nameof(parameterName));
            }

            if(!_parts.TryGetValue(parameterName, out string? value1)) {
                throw new ArgumentException($"connection string requires '{parameterName}' parameter.");
            }

            if(requireNonEmptyValue && string.IsNullOrEmpty(value1)) {
                throw new ArgumentException($"parameter '{parameterName}' is present but value is not set.");
            }

            value = value1;
        }

        /// <summary>
        /// Get connection string parameter by name
        /// </summary>
        /// <param name="parameterName"></param>
        /// <returns>Parameter value. If parameter is not set returns null.</returns>
        public string? Get(string parameterName) {
            if(parameterName == null)
                return null;
            if(!_parts.TryGetValue(parameterName, out string? value))
                return null;
            return value;
        }

        private string ParseAndGetPrefix(string connectionString) {
            int idx = connectionString.IndexOf(PrefixSeparator);

            if(idx == -1) {
                return connectionString;
            }

            string prefix = connectionString.Substring(0, idx);
            connectionString = connectionString.Substring(idx + PrefixSeparator.Length);

            // prefix extracted, now get the parts of the string

            //check if this is a native connection string
            if(connectionString.StartsWith(KnownParameter.Native + "=")) {
                _parts[KnownParameter.Native] = connectionString.Substring(KnownParameter.Native.Length + 1);
            } else {
                string[] parts = connectionString.Split(PartsSeparators, StringSplitOptions.RemoveEmptyEntries);
                foreach(string part in parts) {
                    string[] kv = part.Split(PartSeparator, 2);

                    string key = kv[0];
                    string value = kv.Length == 1 ? string.Empty : kv[1];
                    _parts[key] = value.UrlDecode()!;
                }
            }

            return prefix;
        }

        /// <summary>
        /// Returns a string representation of the connection string
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append(Prefix);
            sb.Append(PrefixSeparator);

            if(_parts.TryGetValue(KnownParameter.Native, out string? nativeConnectionString)) {
                sb.Append(KnownParameter.Native);
                sb.Append(PartSeparator);
                sb.Append(nativeConnectionString);
            } else {

                bool first = true;
                foreach(KeyValuePair<string, string> pair in _parts) {
                    if(first) {
                        first = false;
                    } else {
                        sb.Append(PartsSeparators);
                        first = false;
                    }

                    sb.Append(pair.Key);
                    sb.Append(PartSeparator);
                    sb.Append(pair.Value.UrlEncode());
                }
            }

            return sb.ToString();
        }
    }
}