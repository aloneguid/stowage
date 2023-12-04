using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Stowage.Impl.Microsoft {
    sealed class AzureWriteStream : PolyfilledWriteStream {
        private readonly AzureBlobFileStorage _parent;
        private readonly IOPath _path;
        private readonly bool _append;
        private int _blockId;
        private readonly List<string> _blockIds = new List<string>();

        public AzureWriteStream(AzureBlobFileStorage parent, IOPath path, bool append) : base(1024 * 1024) {
            _parent = parent;
            _path = path;
            _append = append;
        }

        protected override void DumpBuffer(byte[] buffer, int count, bool isFinal) {
            if(_append) {
                try {
                    _parent.AppendBlock(_path, buffer, count);
                } catch(FileNotFoundException) {
                    _parent.PutBlob(_path, "AppendBlob");
                    _parent.AppendBlock(_path, buffer, count);
                }
            } else {
                _blockIds.Add(_parent.PutBlock(_blockId++, _path, buffer, count));

            }
        }

        protected override async Task DumpBufferAsync(byte[] buffer, int count, bool isFinal) {
            if(_append) {
                try {
                    await _parent.AppendBlockAsync(_path, buffer, count);
                } catch(FileNotFoundException) {
                    await _parent.PutBlobAsync(_path, "AppendBlob");
                    await _parent.AppendBlockAsync(_path, buffer, count);
                }
            } else {
                _blockIds.Add(await _parent.PutBlockAsync(_blockId++, _path, buffer, count));

            }
        }

        protected override void Commit() {
            // no need to commit append blobs
            if(!_append) {
                _parent.PutBlockList(_path, _blockIds);
            }
        }

        protected override Task CommitAsync() {
            // no need to commit append blobs
            if(!_append) {
                return _parent.PutBlockListAsync(_path, _blockIds);
            }

            return Task.CompletedTask;
        }
    }
}