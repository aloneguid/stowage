using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Stowage.Test
{
   public class ChunkingReadStreamTest
   {
      private readonly byte[] _data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };



      [Theory]
      [InlineData(10, 2)]
      [InlineData(10, 7)]
      [InlineData(10, 12)]
      public void Read_VarialbleLength_VariableMaxChunk(int dataLength, int chunkLength)
      {
         byte[] buffer = new byte[100];
         byte[] data = new byte[dataLength];
         Array.Copy(_data, data, dataLength);

         using(var imrs = new InMemoryReadStream(data, chunkLength))
         {
            Assert.Equal(dataLength, imrs.Length);
            int read = imrs.Read(buffer, 0, buffer.Length);

            Assert.Equal(dataLength, read);
            Assert.Equal(data, buffer.Take(dataLength).ToArray());
            Assert.Equal(read, imrs.Position);
         }
      }
   }

   class InMemoryReadStream : ChunkingReadStream
   {
      private readonly byte[] _data;

      public InMemoryReadStream(byte[] data, long maxChunkLength) : base(maxChunkLength, data.Length)
      {
         _data = data;
      }

      protected override int SmallRead(long globalPos, byte[] buffer, int offset, int count)
      {
         int left = (int)(_data.Length - globalPos);
         int canRead = Math.Min(left, count);

         Array.Copy(_data, globalPos, buffer, offset, canRead);

         return canRead;
      }

      protected override Task<int> SmallReadAsync(long globalPos, byte[] buffer, int offset, int count) =>
         Task.FromResult(SmallRead(globalPos, buffer, offset, count));
   }
}
