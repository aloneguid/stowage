using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Stowage.Impl.Microsoft;
using Xunit;

namespace Stowage.Test.Integration.Impl {
    [Trait("Category", "Integration")]
    public class AzureBlobTest {
        private readonly ITestSettings _settings;
        private readonly string _prefix;
        private readonly IAzureBlobFileStorage _storage;

        public AzureBlobTest() {
            _settings = ConfigLoader.Load();

            _storage = (IAzureBlobFileStorage)Files.Of.AzureBlobStorage(_settings.AzureStorageAccount, _settings.AzureStorageKey);
            _prefix = _settings.AzureContainerName + "/";
        }

        [Fact]
        public async Task OpenWrite_Append_LargerAndLarger() {
            string path = $"{_prefix}{nameof(OpenWrite_Append_LargerAndLarger)}.txt";

            await _storage.Rm(path);

            // write first chunk
            using(Stream s = await _storage.OpenAppend(path)) {
                byte[] line1 = Encoding.UTF8.GetBytes("one");
                await s.WriteAsync(line1, 0, line1.Length);
            }

            // validate
            string? content = await _storage.ReadText(path);
            Assert.Equal("one", content);

            // write second chunk
            using(Stream s = await _storage.OpenAppend(path)) {
                byte[] line1 = Encoding.UTF8.GetBytes("two");
                await s.WriteAsync(line1, 0, line1.Length);
            }

            // validate
            content = await _storage.ReadText(path);
            Assert.Equal("onetwo", content);
        }

        [Fact]
        public async Task ListContainers_NotEmpty() {
            IReadOnlyCollection<IOEntry> containers = await _storage.Ls(IOPath.Root);
            Assert.NotEmpty(containers);
        }

        [Fact]
        public async Task Auth_ClientCredentials() {
            IFileStorage storage = Files.Of.AzureBlobStorage(
                _settings.AzureStorageAccount,
                new ClientSecretCredential(_settings.AzureTenantId, _settings.AzureClientId, _settings.AzureClientSecret));

            await storage.Ls();
        }
    }
}