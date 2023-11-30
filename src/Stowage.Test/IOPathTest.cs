using Xunit;

namespace Stowage.Test {
    public class IOPathTest {
        [Theory]
        [InlineData("/dev/one", new[] { "dev", "one" })]
        [InlineData("/one/two/three", new[] { "one", "two", "three" })]
        [InlineData("/three", new[] { "one", "..", "three" })]
        [InlineData("/dev/one/", new[] { "dev", "one/" })]
        public void Combine_theory(string expected, string[] parts) {
            Assert.Equal(expected, IOPath.Combine(parts));
        }

        [Theory]
        [InlineData("file", "container", "/container/file")]
        [InlineData("file/", "container", "/container/file/")]
        [InlineData("/", "container", "/container/")]
        public void Prefix_theory(string path, string prefix, string expected) {
            IOPath pathPath = path;
            IOPath prefixPath = prefix;
            Assert.Equal(expected, pathPath.Prefix(prefixPath).Full);
        }

        [Theory]
        [InlineData(new[] { "one", "two" }, "one/two")]
        [InlineData(new[] { "one", "two" }, "/one/two")]
        [InlineData(new[] { "one", "two/" }, "one/two/")]
        [InlineData(new[] { "one", "two/" }, "/one/two/")]
        public void Split_theory(string[] expected, string input) {
            Assert.Equal(expected, IOPath.Split(input));
        }

        [Theory]
        [InlineData("dev/..", "/")]
        [InlineData("dev/../storage", "/storage")]
        [InlineData("/one", "/one")]
        [InlineData("/one/", "/one/")]
        [InlineData("/one/../../../..", "/")]
        public void Normalize_theory(string path, string expected) {
            Assert.Equal(expected, IOPath.Normalize(path));
        }

        [Theory]
        [InlineData("dev1", "/dev1/")]
        public void Normalize_trailing_theory(string path, string expected) {
            Assert.Equal(expected, IOPath.Normalize(path, appendTrailingSlash: true));
        }

        [Theory]
        [InlineData("one/two/three", "/one/two/")]
        [InlineData("one/two", "/one/")]
        [InlineData("one/../two/three", "/two/")]
        [InlineData("one/../two/three/four/..", "/two/")]
        public void Get_parent_theory(string path, string expected) {
            Assert.Equal(expected, IOPath.GetParent(path));
        }

        [Theory]
        [InlineData("/one/two", "/one", "/two")]
        [InlineData("/one/two/", "/one", "/two/")]
        [InlineData("/one/two", "one", "/two")]
        [InlineData("/one/two", "/", "/one/two")]
        [InlineData("/one/two", "x", "/")]
        [InlineData("/one/two", "/1/2/3/4", "/")]
        [InlineData("/one/two", null, "/")]
        [InlineData(null, null, "/")]
        [InlineData(null, "/", "/")]
        public void Relative_theory(string path, string relativeTo, string expected) {
            Assert.Equal(expected, IOPath.RelativeTo(path, relativeTo));
        }

        [Theory]
        [InlineData("/", null, true)]
        [InlineData(null, "/", true)]
        [InlineData("/", "", true)]
        [InlineData("/path1", "path1", true)]
        public void Compare_theory(string path1, string path2, bool expected) {
            Assert.Equal(expected, IOPath.Compare(path1, path2));
        }

        [Theory]
        [InlineData("/one/", "/one/")]
        [InlineData("/one", "/one/")]
        [InlineData("one/", "/one/")]
        public void WTS_theory(string input, string expected) {
            Assert.Equal(expected, new IOPath(input).WTS);
        }

        [Theory]
        [InlineData("/one/", "one/")]
        [InlineData("/one", "one")]
        [InlineData("one/", "one/")]
        public void NLS_theory(string input, string expected) {
            Assert.Equal(expected, new IOPath(input).NLS);
        }

        [Theory]
        [InlineData("/one/", "one/")]
        [InlineData("/one", "one/")]
        [InlineData("one/", "one/")]
        public void NLWTS_theory(string input, string expected) {
            Assert.Equal(expected, new IOPath(input).NLWTS);
        }

        [Theory]
        [InlineData("/", "/", "/")]
        [InlineData("/one/two", "one", "/two")]
        [InlineData("/one/two/", "one", "/two/")]
        [InlineData("/one/", "one", "/")]
        public void ExtractPrefixAndRelativePath_Theory(string input, string expectedPrefix, string expectedRelativePath) {
            new IOPath(input).ExtractPrefixAndRelativePath(out string prefix, out IOPath relativePath);
            Assert.Equal(expectedPrefix, prefix);
            Assert.Equal(expectedRelativePath, relativePath.Full);
        }

        [Theory]
        [InlineData("/one/two", "/one", "/two")]
        public void RelativeTo_theory(string input, string relativeTo, string expectation) {
            Assert.Equal(expectation, new IOPath(input).RelativeTo(relativeTo).Full);
        }
    }
}