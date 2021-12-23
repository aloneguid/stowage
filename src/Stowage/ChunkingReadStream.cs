using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage
{
   abstract class ChunkingReadStream : Stream
   {
      private const long ReadBlockLength = 1024 * 1024; // 1 Mb
      private readonly long _maxChunkLength;
      private readonly long _length;
      private long _pos;

      public ChunkingReadStream(long maxChunkLength, long length)
      {
         _maxChunkLength = maxChunkLength;
         _length = length;
      }

      public override int Read(byte[] buffer, int offset, int count)
      {
         if(Files.HasLogger)
         {
            Files.Log($"read: offset: {offset}, count: {count}");
         }

         int readTotal = 0;
         do
         {
            int toRead = (int)Math.Min(_maxChunkLength, count - readTotal);
            if(Files.HasLogger)
            {
               Files.Log($"to read: {toRead}");
            }
            if(toRead <= 0)
               return readTotal;
            int read = SmallRead(_pos + readTotal, buffer, offset + readTotal, toRead);
            readTotal += read;
            if(read < toRead)
               break;
         } while(true);

         _pos += readTotal;

         if(Files.HasLogger)
         {
            Files.Log($"read {readTotal}");
         }

         return readTotal;
      }

      public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
      {
         if(Files.HasLogger)
         {
            Files.Log($"read: offset: {offset}, count: {count}, length: {_length}");
         }

         int readTotal = 0;
         do
         {
            int toRead = (int)Math.Min(_maxChunkLength, count - readTotal);
            if(Files.HasLogger)
            {
               Files.Log($"to read: {toRead}");
            }
            if(toRead <= 0)
               break;
            int read = await SmallReadAsync(_pos + readTotal, buffer, offset + readTotal, toRead);
            readTotal += read;
            if(read < toRead)
               break;
         } while(true);

         _pos += readTotal;
         return readTotal;
      }

      protected abstract int SmallRead(long globalPos, byte[] buffer, int offset, int count);

      protected abstract Task<int> SmallReadAsync(long globalPos, byte[] buffer, int offset, int count);

      public override bool CanRead => true;

      public override bool CanSeek => true;

      public override bool CanWrite => false;

      public override long Length => _length;

      public override long Position { get => _pos; set => throw new NotSupportedException(); }

      public override void Flush() { }

      public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
      public override void SetLength(long value) => throw new NotSupportedException();
      public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
   }
}
