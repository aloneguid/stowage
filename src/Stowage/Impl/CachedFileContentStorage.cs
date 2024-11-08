using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl {
    /// <summary>
    /// Storage with a backing cache for content reading. Only reads are cached (<see cref="OpenRead(IOPath, CancellationToken)"/>
    /// and nothing else. This means that the parent storage is always the source of truth, and will always be invoked even 
    /// on read operation in order to check if we still have the valid copy locally
    /// </summary>
    class CachedFileContentStorage : PolyfilledFileStorage, ICachedStorage {

        private readonly IFileStorage _parent;
        private readonly IFileStorage _cachingBackend;
        private readonly TimeSpan _maxAge;
        private readonly bool _cleanupOnDispose;
        private readonly bool _disposeCachingBackend;

        public CachedFileContentStorage(IFileStorage parent, IFileStorage cachingBackend,
            TimeSpan maxAge,
            bool cleanupOnDispose,
            bool disposeCachingBackend) {
            _parent = parent;
            _cachingBackend = cachingBackend;
            _maxAge = maxAge;
            _cleanupOnDispose = cleanupOnDispose;
            _disposeCachingBackend = disposeCachingBackend;
        }

        async Task Cleanup(TimeSpan maxAge, CancellationToken cancellationToken) {
            IReadOnlyCollection<IOEntry> allEntries = await _cachingBackend.Ls(null, true, cancellationToken);
            foreach(IOEntry entry in allEntries) {
                if(entry.LastModificationTime == null || DateTime.UtcNow - entry.LastModificationTime.Value >= maxAge) {
                    await _cachingBackend.Rm(entry.Path, cancellationToken);
                }
            }
        }

        private static IOPath GetCachingPath(IOEntry entry) {
            if(entry.LastModificationTime == null)
                throw new ArgumentException($"Entry needs to have a {nameof(IOEntry.LastModificationTime)} in order to determine cache validity", nameof(entry));
            return new IOPath(entry.Path, entry.LastModificationTime.Value.Ticks.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose() {

            if(_cleanupOnDispose) {
                Cleanup(_maxAge, CancellationToken.None).Forget();
            }

            base.Dispose();

            _parent.Dispose();

            if(_disposeCachingBackend) {
                _cachingBackend.Dispose();
            }
        }

        public override async Task<Stream?> OpenRead(IOPath path, CancellationToken cancellationToken = default) {

            IOEntry? entryNow = await _parent.Stat(path, cancellationToken);
            if(entryNow == null) {
                // entry does not exist
                return null;
            }

            if(entryNow.LastModificationTime == null)
                throw new ArgumentException($"Entry needs to have a {nameof(IOEntry.LastModificationTime)} in order to determine cache validity", nameof(entryNow));

            TimeSpan age = DateTime.UtcNow - entryNow.LastModificationTime.Value;
            IOPath cachePath = GetCachingPath(entryNow);

            if(age >= _maxAge) {
                await _cachingBackend.Rm(cachePath, cancellationToken);
            }

            // create cached entry
            if(!await _cachingBackend.Exists(cachePath, cancellationToken)) {
                using Stream? src = await _parent.OpenRead(path, cancellationToken);
                if(src == null) {
                    return null;
                }

                // copy file to caching backend
                using Stream dest = await _cachingBackend.OpenWrite(cachePath, cancellationToken);
                await src.CopyToAsync(dest, cancellationToken);
            }

            return await _cachingBackend.OpenRead(cachePath, cancellationToken);
        }

        public override Task<IReadOnlyCollection<IOEntry>> Ls(IOPath? path = null, bool recurse = false, CancellationToken cancellationToken = default) =>
            _parent.Ls(path, recurse, cancellationToken);

        public override Task<Stream> OpenWrite(IOPath path, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public override async Task Rm(IOPath path, CancellationToken cancellationToken = default) {
            await _parent.Rm(path, cancellationToken);
            await Invaliadate(path, cancellationToken);
        }

        public override Task<IOEntry?> Stat(IOPath path, CancellationToken cancellationToken = default) =>
            _parent.Stat(path, cancellationToken);

        public async Task<bool> Invaliadate(IOPath path, CancellationToken cancellationToken) {
            bool dirty = false;

            // we are going to list all the versions of the file and remove them
            IReadOnlyCollection<IOEntry> entries = await _cachingBackend.Ls(path + IOPath.PathSeparatorString, false, cancellationToken);
            foreach(IOEntry entry in entries) {
                await _cachingBackend.Rm(entry.Path, cancellationToken);
                dirty = true;
                if(cancellationToken.IsCancellationRequested)
                    break;
            }

            return dirty;
        }

        public Task Clear(CancellationToken cancellationToken) =>
            Cleanup(TimeSpan.Zero, cancellationToken);
    }
}
