using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stowage.Impl;
using Xunit;

namespace Stowage.Test.Integration.Impl {
    [Trait("Category", "Integration")]
    public class LocalDiskTest {
        private readonly ILocalDiskFileStorage _storage;

        public LocalDiskTest() {
            _storage = (ILocalDiskFileStorage)Files.Of.LocalDisk(Environment.CurrentDirectory);
        }

        [Fact]
        public async Task ResolveToNativePath() {
            IReadOnlyCollection<IOEntry> entries = await _storage.Ls();
            IOEntry entry = entries.First();

            string nativePath = _storage.ToNativeLocalPath(entry.Path);

            Assert.True(nativePath.Length > 0);
        }
    }
}
