using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Stowage.Test.Integration.Impl {

    [Trait("Category", "Integration")]
    public class LocalCacheStorageTest {
        private readonly IFileStorage _parent;
        private readonly ICachedStorage _storage;

        public LocalCacheStorageTest() {
            _parent = Files.Of.InternalMemory();
            _storage = Files.Of.LocalDiskCacheStorage(_parent);
        }

        [Fact]
        public async Task ReadCached() {
            await _parent.WriteText(nameof(ReadCached), "test");

            string? contentBeforeRm = await _storage.ReadText(nameof(ReadCached));
            Assert.Equal("test", contentBeforeRm);
        }

        [Fact]
        public async Task DeletionWillNotReadCached() {

            string path = new IOPath(nameof(DeletionWillNotReadCached));

            await _parent.WriteText(path, "test");

            // state before
            Assert.True(await _storage.Exists(path));
            Assert.Equal("test", await _storage.ReadText(path));

            // delete entry in parent backend
            await _parent.Rm(path);

            // check entry is not in caching backend
            Assert.False(await _storage.Exists(path));
        }
    }
}
