using System;
using System.Threading.Tasks;

namespace Stowage.Impl.Databricks
{
   class ReadStream : ChunkingReadStream
   {
      private const long ReadBlockLength = 1024 * 1024; // 1 Mb

      private readonly DatabricksRestClient _parent;
      private readonly IOPath _path;

      public ReadStream(DatabricksRestClient parent, IOPath path, long length) : base(ReadBlockLength, length)
      {
         _parent = parent;
         _path = path;
      }

      protected override int SmallRead(long globalPos, byte[] buffer, int offset, int count)
      {
         byte[] data = _parent.Read(_path, globalPos, count);

         Array.Copy(data, 0, buffer, offset, data.Length);

         return data.Length;
      }

      protected override async Task<int> SmallReadAsync(long globalPos, byte[] buffer, int offset, int count)
      {
         byte[] data = await _parent.ReadAsync(_path, globalPos, count);

         Array.Copy(data, 0, buffer, offset, data.Length);

         return data.Length;
      }
   }
}
