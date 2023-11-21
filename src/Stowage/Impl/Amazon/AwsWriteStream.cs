using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Amazon;

sealed class AwsWriteStream : PolyfilledWriteStream {
    private readonly AwsS3FileStorage _parent;
    private readonly string _key;
    private readonly MemoryStream _stream;

    public AwsWriteStream(AwsS3FileStorage parent, string key) : base(1024 * 1024) {
        _parent = parent;
        _key = key;
        _stream = new MemoryStream();
    }

    protected override void DumpBuffer(byte[] buffer, int count, bool isFinal) {
        _stream.Write(buffer, 0, count);
    }

    protected override Task DumpBufferAsync(byte[] buffer, int count, bool isFinal) {
        return _stream.WriteAsync(buffer, 0, count, CancellationToken.None);
    }


    protected override void Commit() {
        _stream.Seek(0, SeekOrigin.Begin);
        _parent.CompleteUpload(_key, _stream);
    }

    protected override Task CommitAsync() {
        _stream.Seek(0, SeekOrigin.Begin);
        return _parent.CompleteUploadAsync(_key, _stream);
    }
}
