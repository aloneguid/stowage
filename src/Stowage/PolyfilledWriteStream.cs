using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage
{
   abstract class PolyfilledWriteStream : Stream, IAsyncDisposable
   {
      private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
      private readonly byte[] _buffer;
      private readonly int _bufferSize;
      private int _bufferPos;
      private bool _disposed = false;
      private static readonly int _envSize;

      protected PolyfilledWriteStream(int bufferSize)
      {
         _buffer = _pool.Rent(bufferSize);

         _bufferSize = _envSize != 0 ? _envSize : bufferSize;
         //_bufferSize = 5;
         Debug.WriteLine("buffer size: {0}", _bufferSize);
      }

      static PolyfilledWriteStream()
      {
         string envSize = Environment.GetEnvironmentVariable(Constants.WriteStreamBufferSizeEnvVarName);
         if(envSize != null)
         {
            int.TryParse(envSize, out _envSize);
         }
      }

      protected long _length;

      public override bool CanRead => false;

      public override bool CanSeek => false;

      public override bool CanWrite => true;

      public override long Length => _length;

      public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

      public override void Flush() { }
      public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
      public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
      public override void SetLength(long value) => throw new NotSupportedException();

      public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

      public override void Write(byte[] buffer, int offset, int count)
      {
         int ingestedTotal = 0;
         int ingested;
         int left = count;
         
         while((left > 0) && ((ingested = Ingest(buffer, offset + ingestedTotal, left)) <= left))
         {
            DumpBuffer(_buffer, _bufferPos, false);
            _bufferPos = 0;
            ingestedTotal += ingested;
            left -= ingested;
         }
      }

      public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
      {
         int ingestedTotal = 0;
         int ingested;
         int left = count;

         while((left > 0) && ((ingested = Ingest(buffer, offset + ingestedTotal, left)) <= left))
         {
            await DumpBufferAsync(_buffer, _bufferPos, false);
            _bufferPos = 0;
            ingestedTotal += ingested;
            left -= ingested;
         }
      }

      private int Ingest(byte[] buffer, int offset, int count)
      {
         int toCopy = Math.Min(count, _bufferSize - _bufferPos);

         Array.Copy(buffer, offset, _buffer, _bufferPos, toCopy);

         _bufferPos += toCopy;
         _length += toCopy;

         return toCopy;
      }

      protected abstract void DumpBuffer(byte[] buffer, int count, bool isFinal);

      protected abstract Task DumpBufferAsync(byte[] buffer, int count, bool isFinal);

      protected override void Dispose(bool disposing)
      {
         if(!_disposed)
         {
            if(_bufferPos > 0)
               DumpBuffer(_buffer, _bufferPos, true);

            _pool.Return(_buffer);

            Commit();

            _disposed = true;
         }

         base.Dispose(disposing);
      }

      public override async ValueTask DisposeAsync()
      {
         if(!_disposed)
         {
            if(_bufferPos > 0)
               await DumpBufferAsync(_buffer, _bufferPos, true);

            _pool.Return(_buffer);

            await CommitAsync();

            _disposed = true;
         }
      }

      protected abstract void Commit();

      protected abstract Task CommitAsync();
   }
}
