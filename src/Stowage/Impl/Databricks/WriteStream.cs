using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// https://docs.databricks.com/dev-tools/api/latest/dbfs.html#put
   /// </summary>
   sealed class WriteStream : PolyfilledWriteStream
   {
      private readonly DatabricksRestClient _parent;
      private readonly long _handle;

      public WriteStream(DatabricksRestClient parent, long handle) : base(1024 * 1024)
      {
         _parent = parent;
         _handle = handle;
      }

      protected override void Commit()
      {
         _parent.Close(_handle);
      }

      protected override Task CommitAsync()
      {
         return _parent.CloseAsync(_handle);
      }

      protected override void DumpBuffer(byte[] buffer, int count, bool isFinal)
      {
         _parent.AddBlock(_handle, buffer, count);
      }

      protected override Task DumpBufferAsync(byte[] buffer, int count, bool isFinal)
      {
         return _parent.AddBlockAsync(_handle, buffer, count);
      }
   }
}
