using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NetBox.Generator;

namespace Stowage.SelfTest {
    /// <summary>
    /// Contains self-testing code.
    /// </summary>
    public class FilesSelfTest {
        private readonly IFileStorage _storage;
        private readonly bool _failOnFirstTest;
        private readonly string? _pathPrefix;
        private readonly List<TestOutcome> _outcomes = new List<TestOutcome>();
        private readonly List<Tuple<string, Func<Task>>> _testMethods;

        private class TestAttribute : Attribute {

        }

        private FilesSelfTest(IFileStorage storage, bool failOnFirstTest = false, string? pathPrefix = null) {
            _storage = storage;
            _failOnFirstTest = failOnFirstTest;
            _pathPrefix = pathPrefix;
            _testMethods = FindTestMethods();

            if(_pathPrefix != null) {
                _pathPrefix = _pathPrefix.Trim('/') + "/";
            }
        }

        private List<Tuple<string, Func<Task>>> FindTestMethods() {
            var r = new List<Tuple<string, Func<Task>>>();

            foreach(MethodInfo mi in GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
                Attribute ta = mi.GetCustomAttribute(typeof(TestAttribute));
                if(ta != null) {
                    // validate
                    if(mi.ReturnType != typeof(Task))
                        throw new ApplicationException($"{mi.Name}: return type must be Task");

                    if(mi.GetParameters().Length > 0)
                        throw new ApplicationException($"{mi.Name}: parameters are not supported");

                    r.Add(new Tuple<string, Func<Task>>(mi.Name, () => (Task)mi.Invoke(this, new object[0])));
                }
            }

            return r;
        }

        /// <summary>
        /// array of object[] where:
        ///  0 - test name
        ///  1 - Func of Task
        /// </summary>
        /// <returns></returns>
        public IEnumerable<object[]> GetXUnitTestData() {
            return _testMethods.Select(m => new object[] { m.Item1, m.Item2 });
        }

        private async Task ExecuteAsync() {
            // delete all before we start

            /*IReadOnlyCollection<string> entries = await Directory.GetFileSystemEntries("/");
            foreach(string entry in entries)
            {
               await File.Delete(entry);
            }*/


            // run the tests

            //await Task.WhenAll(_testMethods.Select(e => ExecuteTestAsync(e.Item1, e.Item2)));

            foreach(Tuple<string, Func<Task>> e in _testMethods) {
                await ExecuteTestAsync(e.Item1, e.Item2);
            }

            int errorCount = _outcomes.Count(o => !o.Success);
            if(errorCount > 0) {
                string errorMessage = string.Join(Environment.NewLine, _outcomes.Where(o => !o.Success));

                throw new Exception(
                   $"{_testMethods.Count} test(s) ran, {_testMethods.Count - errorCount} succeeded, {errorCount} failed. Breakdown:{Environment.NewLine}{errorMessage}");
            }
        }

        private async Task ExecuteTestAsync(string name, Func<Task> testMethod) {
            var outcome = new TestOutcome { TestName = name };

            try {
                await testMethod();
            } catch(ApplicationException ex) {
                outcome.AssertionFailure = true;
                outcome.Expected = ex.Data["expected"]?.ToString();
                outcome.Actual = ex.Data["actual"]?.ToString();

                if(_failOnFirstTest) {
                    throw;
                }
            } catch(Exception ex) {
                outcome.RuntimeException = ex;
                if(_failOnFirstTest) {
                    throw;
                }
            } finally {
                _outcomes.Add(outcome);
            }
        }

        public static Task ExecuteAsync(IFileStorage storage, string? pathPrefix = null) {
            return new FilesSelfTest(storage, true, pathPrefix).ExecuteAsync();
        }

        public static IEnumerable<object[]> GetXUnitTestData(IFileStorage storage, string? pathPrefix = null) {
            return new FilesSelfTest(storage, false, pathPrefix).GetXUnitTestData();
        }


        private void AssertFail(string expected, string actual) {
            var ae = new ApplicationException($"assertion failure: expected - {expected}, actual - {actual}.");
            ae.Data["expected"] = expected;
            ae.Data["actual"] = actual;
            throw ae;
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

        [Test]
        private async Task Ls_NullPath_DoesNotFail() {
            await _storage.Ls(null);
        }

        [Test]
        private async Task Ls_NonFolder_ArgEx() {
            try {
                await _storage.Ls("/afile");
                AssertFail(nameof(ArgumentException), "no failures");
            } catch(ArgumentException) {

            }
        }

        [Test]
        private async Task Ls_NoParamsAtAll_NoCrash() {
            await _storage.Ls();
        }

        [Test]
        private async Task Ls_Root_NoCrash() {
            await _storage.Ls(IOPath.RootFolderPath);
        }

        [Test]
        private async Task Ls_WriteTwoFiles_TwoFilesMore() {
            int preCount = (await _storage.Ls(_pathPrefix)).Count;

            string id1 = await GetRandomStreamIdAsync();
            string id2 = await GetRandomStreamIdAsync();

            IReadOnlyCollection<IOEntry> entries = await _storage.Ls(_pathPrefix);
            int postCount = entries.Count;

            if(postCount != preCount + 2)
                AssertFail((preCount + 2).ToString(), postCount.ToString());
        }

        [Test]
        private async Task Ls_WriteFileInAFolderAndListRecursively_ReturnsExtraFolderAndExtraFile() {
            int preCount = (await _storage.Ls(_pathPrefix, recurse: true)).Count;

            string folderName = Guid.NewGuid().ToString();
            string f1 = await GetRandomStreamIdAsync(subfolder: folderName);

            IReadOnlyCollection<string> postList = (await _storage.Ls(_pathPrefix, recurse: true)).Select(e => e.Path.Full).ToList();
            int postCount = postList.Count;

            if(postCount != preCount + 2)
                AssertFail((preCount + 2).ToString(), postCount.ToString());

            if(!postList.Contains(new IOPath(_pathPrefix, folderName).WTS))
                AssertFail("contain folder " + folderName, "does not!");

            if(!postList.Contains(f1))
                AssertFail("contain file " + f1, "does not!");
        }

        [Test]
        private async Task Ls_WriteFileInAFolderAndListNonRecursively_ReturnsExtraFolderOnly() {
            int preCount = (await _storage.Ls(_pathPrefix, recurse: false)).Count;

            string folderName = Guid.NewGuid().ToString();
            string f1 = await GetRandomStreamIdAsync(null, folderName);

            IReadOnlyCollection<string> postList = (await _storage.Ls(_pathPrefix, recurse: false)).Select(e => e.Path.Full).ToList();
            int postCount = postList.Count;

            if(postCount != preCount + 1)
                AssertFail((preCount + 1).ToString(), postCount.ToString());

            if(!postList.Contains(new IOPath(_pathPrefix, folderName).WTS))
                AssertFail("contain folder " + folderName, "does not!");

            if(postList.Contains(f1))
                AssertFail("not to contain file " + f1, "does not!");
        }

        [Test]
        private async Task Ls_Recursive_Recurses() {
            string f1 = await GetRandomStreamIdAsync(Guid.NewGuid().ToString());

            IReadOnlyCollection<string> entries = (await _storage.Ls(_pathPrefix ?? IOPath.RootFolderPath, recurse: true)).Select(e => e.Path.Full).ToList();

            bool contains = entries.Contains(f1);

            if(!contains)
                AssertFail($"contain {f1}", "not in the list");
        }

        [Test]
        private async Task Ls_Subfolder_Lists() {
            int preCount = (await _storage.Ls(_pathPrefix)).Count;

            string folderName = Guid.NewGuid().ToString();
            string f1 = await GetRandomStreamIdAsync(null, folderName);

            IReadOnlyCollection<string> entries = (await _storage.Ls(IOPath.Combine(_pathPrefix, folderName) + "/", false)).Select(e => e.Path.Full).ToList();

            if(entries.Count != 1 || entries.First() != f1)
                AssertFail($"a single entry {f1}", $"{entries.Count} entry(ies), first: {entries.FirstOrDefault()}");
        }


        [Test]
        public async Task OpenRead_DoesntExist_ReturnsNull() {
            string id = RandomBlobPath();

            using Stream? s = await _storage.OpenRead(id);

            if(s != null)
                AssertFail("null stream", s.ToString()!);
        }

        [Test]
        public async Task OpenRead_Existing_NotNull() {
            string id = await GetRandomStreamIdAsync();

            using Stream? s = await _storage.OpenRead(id!);

            if(s == null)
                AssertFail("some instance", "null stream");
        }

        [Test]
        public async Task OpenWrite_NullPath_ThrowsArgumentNull() {
            try {
                await _storage.OpenWrite(null);

                AssertFail("exception", "nothing happened");
            } catch(ArgumentNullException) {

            }
        }

        [Test]
        public async Task OpenWrite_WriteSync_DisposeSync_ReadsSameText() {
            string text = "write me here on " + DateTime.UtcNow;

            using(Stream s = await _storage.OpenWrite(IOPath.Combine(_pathPrefix, "writeme.txt"))) {
                byte[] data = Encoding.UTF8.GetBytes(text);
                s.Write(data, 0, data.Length);
            }

            string? actual = await _storage.ReadText(IOPath.Combine(_pathPrefix, "writeme.txt"));
            if(actual != text)
                AssertFail(text, actual);
        }

        [Test]
        public async Task OpenWrite_Write10Mb() {
            int size = 1024 * 1024 * 10;
            byte[] data = RandomGenerator.GetRandomBytes(size, size);

            var path = new IOPath(_pathPrefix, "10mb.bin");
            using(Stream s = await _storage.OpenWrite(path)) {
                s.Write(data, 0, data.Length);
            }
        }


        [Test]
        public async Task OpenWrite_WriteAsync_DisposeSync_ReadsSameText() {
            string text = "write me here on " + DateTime.UtcNow;

            string path = IOPath.Combine(_pathPrefix, "writeme.txt");
            using(Stream s = await _storage.OpenWrite(path)) {
                byte[] data = Encoding.UTF8.GetBytes(text);
                await s.WriteAsync(data, 0, data.Length);
            }

            string? actual = await _storage.ReadText(path);
            if(actual != text)
                AssertFail(text, actual);
        }

#if(NETSTANDARD2_1 || NETCOREAPP3_1_OR_GREATER)
        [Test]
        public async Task OpenWrite_WriteAsync_DisposeAsync_ReadsSameText() {
            string text = "write me here on " + DateTime.UtcNow;

            var path = new IOPath(_pathPrefix, "writeme.txt");
            await using(Stream s = await _storage.OpenWrite(path)) {
                byte[] data = Encoding.UTF8.GetBytes(text);
                await s.WriteAsync(data, 0, data.Length);
            }

            string? actual = await _storage.ReadText(path);
            if(actual != text)
                AssertFail(text, actual);
        }

#endif

        [Test]
        private async Task WriteText_NotAFile_ArgumentException() {
            try {
                await _storage.WriteText("/afolder/", "fake");

                AssertFail(nameof(ArgumentException), "success");
            } catch(ArgumentException) {

            }
        }

        [Test]
        private async Task WriteText_ReadsSameText() {
            string generatedContent = Guid.NewGuid().ToString();

            var path = new IOPath(_pathPrefix, "me.txt");
            await _storage.WriteText(path, generatedContent);

            string? content = await _storage.ReadText(path);

            if(content != generatedContent)
                AssertFail(generatedContent, content);
        }

        [Test]
        private async Task WriteText_SpacesInFilename_ReadsSameText() {
            string generatedContent = Guid.NewGuid().ToString();

            string id = IOPath.Combine(_pathPrefix, "my space.txt");

            await _storage.WriteText(id, generatedContent);

            string? content = await _storage.ReadText(id);

            if(content != generatedContent)
                AssertFail(generatedContent, content);
        }

        [Test]
        private async Task WriteTextInSubfolder_ReadsSameText() {
            string generatedContent = Guid.NewGuid().ToString();

            var path = new IOPath(_pathPrefix, "sub", "me.txt");
            await _storage.WriteText(path, generatedContent);

            string? content = await _storage.ReadText(path);

            if(content != generatedContent)
                AssertFail(generatedContent, content);
        }

        [Test]
        private async Task WriteText_Subfolder_ReadsSameText() {
            string generatedContent = Guid.NewGuid().ToString();

            var path = new IOPath(_pathPrefix, "me.txt");
            await _storage.WriteText(path, generatedContent);

            string? content = await _storage.ReadText(path);

            if(content != generatedContent)
                AssertFail(generatedContent, content);
        }

        [Test]
        private async Task WriteText_NullPath_Fails() {
            try {
                await _storage.WriteText(null, "some");

                AssertFail("exception", "none");
            } catch(ArgumentNullException) {

            }
        }

        [Test]
        private async Task WriteText_NullContent_Fails() {
            try {
                await _storage.WriteText("1.txt", (string)null);

                AssertFail("exception", "none");
            } catch(ArgumentNullException) {

            }
        }

        [Test]
        private async Task ReadText_NotAFile_ArgumentException() {
            try {
                await _storage.ReadText("/afolder/");

                AssertFail(nameof(ArgumentException), "success");
            } catch(ArgumentException) {

            }
        }

        class TestObject {
            public string Name { get; set; }
        }

        [Test]
        private async Task Json_Write_ReadsSameJson() {
            var t0 = new TestObject { Name = "name1" };
            var path = new IOPath(_pathPrefix, "1.json");
            await _storage.WriteAsJson(path, t0);
            TestObject? t1 = await _storage.ReadAsJson<TestObject>(path);

            if(t0.Name != t1.Name)
                AssertFail(t0.Name, t1.Name);
        }

        [Test]
        private async Task Rm_NullPath_ArgumentNullException() {
            try {
                await _storage.Rm(null);

                AssertFail(typeof(ArgumentNullException).ToString(), "nothing");
            } catch(ArgumentNullException) {

            }
        }

        [Test]
        private async Task Rm_OneFile_Deletes() {
            string path = await GetRandomStreamIdAsync();

            await _storage.Rm(path);

            IReadOnlyCollection<string> entries = (await _storage.Ls(_pathPrefix)).Select(e => e.Path.Full).ToList();

            if(entries.Contains(path))
                AssertFail($"not to contain [{path}]", "contains");
        }

        [Test]
        private async Task Rm_TwoFiles_Deletes() {
            string path1 = await GetRandomStreamIdAsync();
            string path2 = await GetRandomStreamIdAsync();

            await _storage.Rm(path1);
            await _storage.Rm(path2);

            IReadOnlyCollection<string> entries = (await _storage.Ls(_pathPrefix)).Select(e => e.Path.Full).ToList();

            if(entries.Contains(path1))
                AssertFail($"not to contain [{path1}]", "contains");

            if(entries.Contains(path2))
                AssertFail($"not to contain [{path1}]", "contains");


        }

        [Test]
        private async Task Rm_Directory_Deletes() {
            string prefix = "Rm_Directory_Deletes";

            string path1 = await GetRandomStreamIdAsync(null, prefix);
            string path2 = await GetRandomStreamIdAsync(null, prefix);

            await _storage.Rm(new IOPath(_pathPrefix, prefix), true);

            IReadOnlyCollection<string> entries = (await _storage.Ls(_pathPrefix)).Select(e => e.Path.Full).ToList();

            if(entries.Contains(prefix))
                AssertFail($"not to contain [{prefix}]", "contains");
        }

        [Test]
        private async Task Rm_FileDoesNotExist_Ignores() {
            await _storage.Rm(new IOPath(_pathPrefix, Guid.NewGuid().ToString()));
        }

        [Test]
        private async Task Exists_Exists_True() {
            string path1 = await GetRandomStreamIdAsync();

            if(!(await _storage.Exists(path1)))
                AssertFail("to exist", "doesn't");
        }

        [Test]
        private async Task Exists_Doesnt_False() {

            string path = new IOPath(_pathPrefix, Guid.NewGuid().ToString());

            if(await _storage.Exists(path))
                AssertFail("not to exist", "exists");
        }

        [Test]
        private async Task Ren_NullName_ArgumentNull() {
            try {
                await _storage.Ren(null, IOPath.Root);

                AssertFail("exception", "pass");
            } catch(ArgumentNullException) {

            }
        }

        [Test]
        private async Task Ren_NullNewName_ArgumentNull() {
            try {
                await _storage.Ren(IOPath.Root, null);

                AssertFail("exception", "pass");
            } catch(ArgumentNullException) {

            }
        }
    }
}