using System;
using System.IO;
using System.Linq;
using NetBox.FileFormats.Ini;

namespace Stowage.Impl.Amazon {
    /// <summary>
    /// Parses credential file from ~/.aws/credentials.
    /// File structure is described here: https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html#cli-configure-files-format-profile
    /// </summary>
    class CredentialFileParser {

        public const string DefaultProfileName = "default";

        private readonly string _credFilePath;
        private readonly string _configFilePath;
        private readonly StructuredIniFile? _credIniFile;
        private readonly StructuredIniFile? _configIniFile;
        private const string AccessKeyIdKeyName = "aws_access_key_id";
        private const string SecretAccessKeyKeyName = "aws_secret_access_key";
        private const string SessionTokenKeyName = "aws_session_token";


        public CredentialFileParser() {
            _credFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws", "credentials");
            _configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws", "config");
            Exists = File.Exists(_credFilePath);
            _credIniFile = Exists ? StructuredIniFile.FromString(File.ReadAllText(_credFilePath)) : null;
            _configIniFile = File.Exists(_configFilePath) ? StructuredIniFile.FromString(File.ReadAllText(_configFilePath)) : null;
        }

        private bool Exists { get; }

        public string[]? ProfileNames => _credIniFile?.SectionNames;

        /// <summary>
        /// Fills profile configuration from config files.
        /// </summary>
        /// <param name="profileName"></param>
        /// <param name="accessKeyId"></param>
        /// <param name="secretAccessKey"></param>
        /// <param name="sessionToken"></param>
        /// <param name="region"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void FillCredentials(string? profileName,
            out string accessKeyId, out string secretAccessKey,
            out string? sessionToken,
            out string? region) {

            if(_credIniFile == null) { 
                throw new InvalidOperationException("Credential file does not exist");
            }

            if(profileName == null)
                profileName = DefaultProfileName;
            
            if(!_credIniFile.SectionNames.Any(s => s == profileName)) {
                throw new InvalidOperationException($"Profile '{profileName}' does not exist in credential file");
            }

            string? accessKeyId1 = _credIniFile[$"{profileName}.{AccessKeyIdKeyName}"];
            string? secretAccessKey1 = _credIniFile[$"{profileName}.{SecretAccessKeyKeyName}"];

            if(accessKeyId1 == null || secretAccessKey1 == null)
                throw new InvalidOperationException($"{AccessKeyIdKeyName} and {SecretAccessKeyKeyName} keys are required");

            accessKeyId = accessKeyId1;
            secretAccessKey = secretAccessKey1;
            sessionToken = _credIniFile[$"{profileName}.{SessionTokenKeyName}"];

            if(_configIniFile != null) {
                // for default profile, the section is called "default", however for non-default profiles
                // the sections are called "profile <profileName>" (weird, huh?)
                string sectionName = profileName == DefaultProfileName ? "default" : $"profile {profileName}";
                region = _configIniFile[$"{sectionName}.region"];
            } else {
                region = null;
            }
        }
    }
}
