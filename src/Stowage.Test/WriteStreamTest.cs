using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Stowage.Test
{
   public class WriteStreamTest
   {
      private readonly byte[] _data = new byte[] { 1, 2, 3, 4, 5, 6, 7 };

      [Fact]
      public void Write_6b_2b()
      {
         var s = new InMemoryWriteStream(2);

         s.Write(_data, 0, 6);

         s.Dispose();

         Assert.Equal(3, s.blocks.Count);
         Assert.True(s.isCommitted);
         Assert.Equal(new byte[] { 1, 2 }, s.blocks[0]);
         Assert.Equal(new byte[] { 3, 4 }, s.blocks[1]);
         Assert.Equal(new byte[] { 5, 6 }, s.blocks[2]);
      }

      [Fact]
      public void Write_7b_2b()
      {
         var s = new InMemoryWriteStream(2);

         s.Write(_data, 0, 7);

         s.Dispose();

         Assert.Equal(4, s.blocks.Count);
         Assert.True(s.isCommitted);
         Assert.Equal(new byte[] { 1, 2 }, s.blocks[0]);
         Assert.Equal(new byte[] { 3, 4 }, s.blocks[1]);
         Assert.Equal(new byte[] { 5, 6 }, s.blocks[2]);
         Assert.Equal(new byte[] { 7 }, s.blocks[3]);
      }

      [Fact]
      public void Write_2b_7b()
      {
         var s = new InMemoryWriteStream(2);

         s.Write(_data, 1, 2);

         s.Dispose();

         Assert.Single(s.blocks);
         Assert.True(s.isCommitted);
         Assert.Equal(new byte[] { 2, 3 }, s.blocks[0]);
      }
   }

   class InMemoryWriteStream : PolyfilledWriteStream
   {
      public List<byte[]> blocks = new List<byte[]>();
      public bool isCommitted = false;

      public InMemoryWriteStream(int bufferSize) : base(bufferSize)
      {
      }

      protected override void DumpBuffer(byte[] buffer, int count, bool isFinal)
      {
         byte[] chunk = new byte[count];
         Array.Copy(buffer, chunk, count);
         blocks.Add(chunk);
      }

      protected override Task DumpBufferAsync(byte[] buffer, int count, bool isFinal)
      {
         DumpBuffer(buffer, count, isFinal);

         return Task.CompletedTask;
      }


      protected override void Commit()
      {
         isCommitted = true;
      }

      protected override Task CommitAsync()
      {
         isCommitted = true;
         return Task.CompletedTask;
      }
   }
}
