using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using NetBox.Generator;

namespace Stowage.Test.Integration {

    public class AwsS3IntegrationTest : UniversalIntegrationTest {
        public AwsS3IntegrationTest() : base("S3") { }
    }

    public class AzureBlobIntegrationTest : UniversalIntegrationTest {
        public AzureBlobIntegrationTest() : base("AzureBlob") { }
    }

    public class MinioIntegrationTest : UniversalIntegrationTest {
        public MinioIntegrationTest() : base("Minio") { }
    }

    public class GcpIntegrationTest : UniversalIntegrationTest {
        public GcpIntegrationTest() : base("GCP") { }
    }

    public class DiskIntegrationTest : UniversalIntegrationTest {
        public DiskIntegrationTest() : base("Disk") { }
    }

    public class MemIntegrationTest : UniversalIntegrationTest {
        public MemIntegrationTest() : base("Mem") { }
    }

    public class DbfsIntegrationTest : UniversalIntegrationTest {
        public DbfsIntegrationTest() : base("DBFS") { }
    }

    [Trait("Category", "Integration")]
    public abstract class UniversalIntegrationTest {
        protected readonly ITestSettings settings = ConfigLoader.Load();
        private readonly string? _pathPrefix = null;
        private readonly IFileStorage _storage;

        public UniversalIntegrationTest(string name) {
            switch(name) {
                case "AzureBlob":
                    _storage = Files.Of.AzureBlobStorage(settings.AzureStorageAccount, settings.AzureStorageKey);
                    _pathPrefix = "/" + settings.AzureContainerName + "/";
                    break;
                //case "AzureTable":
                //   storage = Files.Of.AzureTableStorage(settings.AzureStorageAccount, settings.AzureStorageKey);
                //   break;
                case "S3":
                    _storage = Files.Of.AmazonS3(settings.AwsKey, settings.AwsSecret, settings.AwsRegion);
                    _pathPrefix = "/" + settings.AwsBucket + "/";
                    break;
                case "Minio":
                    _storage = Files.Of.Minio(settings.MinioEndpoint, settings.MinioKey, settings.MinioSecret);
                    _pathPrefix = "/" + settings.MinioBucket + "/";
                    break;
                case "GCP":
                    _storage = Files.Of.GoogleCloudStorage(settings.GcpBucket, settings.GcpCred.Base64Decode());
                    break;
                case "Disk":
                    string dirPath = "c:\\tmp\\storage-io-files";

                    if(Directory.Exists(dirPath))
                        Directory.Delete(dirPath, true);

                    _storage = Files.Of.LocalDisk(dirPath);
                    break;
                case "Mem":
                    _storage = Files.Of.InternalMemory(Guid.NewGuid().ToString());
                    break;
                case "DBFS":
                    _storage = Files.Of.DatabricksDbfs(settings.DatabricksBaseUri, settings.DatabricksToken);
                    _pathPrefix = "/FileStore/itest";
                    break;

                default:
                    throw new ArgumentException($"what's '{name}'?");
            }

        }

        private string RandomBlobPath(string? filenamePrefix = null, string? subfolder = null, string extension = "") {
            string id = IOPath.Combine(
               subfolder,
               (filenamePrefix ?? "") + Guid.NewGuid().ToString() + extension);

            if(_pathPrefix != null)
                id = IOPath.Combine(_pathPrefix, id);

            return id;
        }

        private async Task<string> GetRandomStreamIdAsync(string? filenamePrefix = null, string? subfolder = null) {
            string id = RandomBlobPath(filenamePrefix, subfolder);

            using Stream ws = await _storage.OpenWrite(id);
            using Stream s = "kjhlkhlkhlkhlkh".ToMemoryStream()!;

            s.CopyTo(ws);

            return id;
        }


        // ----- THE ACTUAL TESTS --------

        [Fact]
        public async Task Ls_NullPath_DoesNotFail() {
            await _storage.Ls(null);
        }

        [Fact]
        public async Task Ls_NonFolder_ArgEx() {
            await Assert.ThrowsAsync<ArgumentException>(() => _storage.Ls("/afile"));
        }

        [Fact]
        public async Task Ls_NoParamsAtAll_NoCrash() {
            await _storage.Ls();
        }

        [Fact]
        public async Task Ls_Root_NoCrash() {
            await _storage.Ls(IOPath.RootFolderPath);
        }

        [Fact]
        public async Task Ls_WriteTwoFiles_TwoFilesMore() {
            int preCount = (await _storage.Ls(_pathPrefix)).Count;

            string id1 = await GetRandomStreamIdAsync();
            string id2 = await GetRandomStreamIdAsync();

            IReadOnlyCollection<IOEntry> entries = await _storage.Ls(_pathPrefix);
            int postCount = entries.Count;

            Assert.Equal(preCount + 2, postCount);
        }

        [Fact]
        public async Task Ls_WriteFileInAFolderAndListRecursively_ReturnsExtraFolderAndExtraFile() {
            int preCount = (await _storage.Ls(_pathPrefix, recurse: true)).Count;

            string folderName = Guid.NewGuid().ToString();
            string f1 = await GetRandomStreamIdAsync(subfolder: folderName);

            IReadOnlyCollection<string> postList = (await _storage.Ls(_pathPrefix, recurse: true)).Select(e => e.Path.Full).ToList();
            int postCount = postList.Count;

            Assert.Equal(preCount + 2, postCount);

            Assert.Contains(new IOPath(_pathPrefix, folderName).WTS, postList);
            Assert.Contains(f1, postList);
        }

        [Fact]
        public async Task Ls_WriteFileInAFolderAndListNonRecursively_ReturnsExtraFolderOnly() {
            int preCount = (await _storage.Ls(_pathPrefix, recurse: false)).Count;

            string folderName = Guid.NewGuid().ToString();
            string f1 = await GetRandomStreamIdAsync(null, folderName);

            IReadOnlyCollection<string> postList = (await _storage.Ls(_pathPrefix, recurse: false)).Select(e => e.Path.Full).ToList();
            int postCount = postList.Count;

            Assert.Equal(preCount + 1, postCount);

            Assert.Contains(new IOPath(_pathPrefix, folderName).WTS, postList);

            Assert.DoesNotContain(f1, postList);
        }

        [Fact]
        public async Task Ls_Recursive_Recurses() {
            string f1 = await GetRandomStreamIdAsync(Guid.NewGuid().ToString());

            IReadOnlyCollection<string> entries = (await _storage.Ls(_pathPrefix ?? IOPath.RootFolderPath, recurse: true)).Select(e => e.Path.Full).ToList();

            Assert.Contains(f1, entries);
        }

        [Fact]
        public async Task Ls_Subfolder_Lists() {
            int preCount = (await _storage.Ls(_pathPrefix)).Count;

            string folderName = Guid.NewGuid().ToString();
            string f1 = await GetRandomStreamIdAsync(null, folderName);

            IReadOnlyCollection<string> entries = (await _storage.Ls(IOPath.Combine(_pathPrefix, folderName) + "/", false)).Select(e => e.Path.Full).ToList();

            if(entries.Count != 1 || entries.First() != f1)
                Assert.False(true, $"a single entry {f1}");
        }

        [Fact]
        public async Task Ls_6_000_ListsAll() {
            int preCount = (await _storage.Ls(_pathPrefix)).Count;
            int add = 6000 - preCount;

            // write 10k files
            for(int i = 0; i < add; i++) {
                await GetRandomStreamIdAsync();
            }

            int postCount = (await _storage.Ls(_pathPrefix)).Count;

            Assert.True(postCount >= 6000);
        }


        [Fact]
        public async Task OpenRead_DoesntExist_ReturnsNull() {
            string id = RandomBlobPath();

            using Stream? s = await _storage.OpenRead(id);
            Assert.Null(s);
        }

        [Fact]
        public async Task OpenRead_Existing_NotNull() {
            string id = await GetRandomStreamIdAsync();

            using Stream? s = await _storage.OpenRead(id!);

            Assert.NotNull(s);
        }

        [Fact]
        public async Task OpenWrite_NullPath_ThrowsArgumentNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.OpenWrite(null));
        }

        [Fact]
        public async Task OpenWrite_WriteSync_DisposeSync_ReadsSameText() {
            string text = "write me here on " + DateTime.UtcNow;

            using(Stream s = await _storage.OpenWrite(IOPath.Combine(_pathPrefix, "writeme.txt"))) {
                byte[] data = Encoding.UTF8.GetBytes(text);
                s.Write(data, 0, data.Length);
            }

            string? actual = await _storage.ReadText(IOPath.Combine(_pathPrefix, "writeme.txt"));
            Assert.Equal(text, actual);
        }

        [Fact]
        public async Task OpenWrite_Write10Mb() {
            int size = 1024 * 1024 * 10;
            byte[] data = RandomGenerator.GetRandomBytes(size, size);

            var path = new IOPath(_pathPrefix, "10mb.bin");
            using(Stream s = await _storage.OpenWrite(path)) {
                s.Write(data, 0, data.Length);
            }
        }


        [Fact]
        public async Task OpenWrite_WriteAsync_DisposeSync_ReadsSameText() {
            string text = "write me here on " + DateTime.UtcNow;

            string path = IOPath.Combine(_pathPrefix, "writeme.txt");
            using(Stream s = await _storage.OpenWrite(path)) {
                byte[] data = Encoding.UTF8.GetBytes(text);
                await s.WriteAsync(data, 0, data.Length);
            }

            string? actual = await _storage.ReadText(path);
            Assert.Equal(text, actual);
        }

#if(NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER)
        [Fact]
        public async Task OpenWrite_WriteAsync_DisposeAsync_ReadsSameText() {
            string text = "write me here on " + DateTime.UtcNow;

            var path = new IOPath(_pathPrefix, "writeme.txt");
            await using(Stream s = await _storage.OpenWrite(path)) {
                byte[] data = Encoding.UTF8.GetBytes(text);
                await s.WriteAsync(data, 0, data.Length);
            }

            string? actual = await _storage.ReadText(path);
            Assert.Equal(text, actual);
        }

#endif

        [Fact]
        public async Task WriteText_NotAFile_ArgumentException() {

            await Assert.ThrowsAsync<ArgumentException>(() => _storage.WriteText("/afolder/", "fake"));
        }

        [Fact]
        public async Task WriteText_ReadsSameText() {
            string generatedContent = Guid.NewGuid().ToString();

            var path = new IOPath(_pathPrefix, "me.txt");
            await _storage.WriteText(path, generatedContent);

            string? content = await _storage.ReadText(path);

            Assert.Equal(content, generatedContent);
        }

        [Fact]
        public async Task WriteText_SpacesInFilename_ReadsSameText() {
            string generatedContent = Guid.NewGuid().ToString();

            string id = IOPath.Combine(_pathPrefix, "my space.txt");

            await _storage.WriteText(id, generatedContent);

            string? content = await _storage.ReadText(id);

            Assert.Equal(content, generatedContent);
        }

        [Fact]
        public async Task WriteTextInSubfolder_ReadsSameText() {
            string generatedContent = Guid.NewGuid().ToString();

            var path = new IOPath(_pathPrefix, "sub", "me.txt");
            await _storage.WriteText(path, generatedContent);

            string? content = await _storage.ReadText(path);

            Assert.Equal(content, generatedContent);
        }

        [Fact]
        public async Task WriteText_Subfolder_ReadsSameText() {
            string generatedContent = Guid.NewGuid().ToString();

            var path = new IOPath(_pathPrefix, "me.txt");
            await _storage.WriteText(path, generatedContent);

            string? content = await _storage.ReadText(path);

            Assert.Equal(content, generatedContent);
        }

        [Fact]
        public async Task WriteText_NullPath_Fails() {

            await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.WriteText(null, "some"));
        }

        [Fact]
        public async Task WriteText_NullContent_Fails() {

            await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.WriteText("1.txt", (string)null));
        }

        [Fact]
        public async Task ReadText_NotAFile_ArgumentException() {

            await Assert.ThrowsAsync<ArgumentException>(() => _storage.ReadText("/afolder/"));
        }

        class TestObject {
            public string Name { get; set; }
        }

        [Fact]
        public async Task Json_Write_ReadsSameJson() {
            var t0 = new TestObject { Name = "name1" };
            var path = new IOPath(_pathPrefix, "1.json");
            await _storage.WriteAsJson(path, t0);
            TestObject? t1 = await _storage.ReadAsJson<TestObject>(path);

            Assert.Equal(t0.Name, t1.Name);
        }

        [Fact]
        public async Task Rm_NullPath_ArgumentNullException() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.Rm(null));
        }

        [Fact]
        public async Task Rm_OneFile_Deletes() {
            string path = await GetRandomStreamIdAsync();

            await _storage.Rm(path);

            IReadOnlyCollection<string> entries = (await _storage.Ls(_pathPrefix)).Select(e => e.Path.Full).ToList();

            Assert.DoesNotContain(path, entries);
        }

        [Fact]
        public async Task Rm_TwoFiles_Deletes() {
            string path1 = await GetRandomStreamIdAsync();
            string path2 = await GetRandomStreamIdAsync();

            await _storage.Rm(path1);
            await _storage.Rm(path2);

            IReadOnlyCollection<string> entries = (await _storage.Ls(_pathPrefix)).Select(e => e.Path.Full).ToList();

            Assert.DoesNotContain(path1, entries);

            Assert.DoesNotContain(path2, entries);
        }

        [Fact]
        public async Task Rm_Directory_Deletes() {
            string prefix = "Rm_Directory_Deletes";

            string path1 = await GetRandomStreamIdAsync(null, prefix);
            string path2 = await GetRandomStreamIdAsync(null, prefix);

            await _storage.Rm(new IOPath(_pathPrefix, prefix));

            IReadOnlyCollection<string> entries = (await _storage.Ls(_pathPrefix)).Select(e => e.Path.Full).ToList();

            Assert.DoesNotContain(prefix, entries);
        }

        [Fact]
        public async Task Rm_FileDoesNotExist_Ignores() {
            await _storage.Rm(new IOPath(_pathPrefix, Guid.NewGuid().ToString()));
        }

        [Fact]
        public async Task Exists_Exists_True() {
            string path = await GetRandomStreamIdAsync();

            Assert.True(await _storage.Exists(path));
        }

        [Fact]
        public async Task Stat_Exists_Some() {
            string path = await GetRandomStreamIdAsync();
            IOEntry? stat = await _storage.Stat(path);
            Assert.NotNull(stat);
        }

        [Fact]
        public async Task Stat_DoesNotExist_Null() {
            string path = RandomBlobPath();
            IOEntry? stat = await _storage.Stat(path);
            Assert.Null(stat);
        }

        [Fact]
        public async Task Exists_Doesnt_False() {

            string path = new IOPath(_pathPrefix, Guid.NewGuid().ToString());

            Assert.False(await _storage.Exists(path));
        }

        [Fact]
        public async Task Ren_NullName_ArgumentNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.Ren(null, IOPath.Root));
        }

        [Fact]
        public async Task Ren_NullNewName_ArgumentNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.Ren(IOPath.Root, null));
        }
    }
}
