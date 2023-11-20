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

        private readonly string _path;
        private readonly StructuredIniFile? _iniFile;

        private const string AccessKeyIdKeyName = "aws_access_key_id";
        private const string SecretAccessKeyKeyName = "aws_secret_access_key";
        private const string SessionTokenKeyName = "aws_session_token";


        public CredentialFileParser() {
            _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws", "credentials");
            Exists = File.Exists(_path);
            _iniFile = Exists ? StructuredIniFile.FromString(File.ReadAllText(_path)) : null;
        }

        private bool Exists { get; }

        public string[]? ProfileNames => _iniFile?.SectionNames;

        public void FillCredentials(string? profileName, out string accessKeyId, out string secretAccessKey, out string? sessionToken) {
            if(_iniFile == null) { 
                throw new InvalidOperationException("Credential file does not exist");
            }

            if(profileName == null)
                profileName = "default";
            
            if(!_iniFile.SectionNames.Any(s => s == profileName)) {
                throw new InvalidOperationException($"Profile '{profileName}' does not exist in credential file");
            }

            string? accessKeyId1 = _iniFile[$"{profileName}.{AccessKeyIdKeyName}"];
            string? secretAccessKey1 = _iniFile[$"{profileName}.{SecretAccessKeyKeyName}"];

            if(accessKeyId1 == null || secretAccessKey1 == null)
                throw new InvalidOperationException($"{AccessKeyIdKeyName} and {SecretAccessKeyKeyName} keys are required");

            accessKeyId = accessKeyId1;
            secretAccessKey = secretAccessKey1;
            sessionToken = _iniFile[$"{profileName}.{SessionTokenKeyName}"];
        }
    }
}
