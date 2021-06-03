using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetBox.IO;

namespace Stowage.Impl.Microsoft
{
   sealed class AzureWriteStream : PolyfilledWriteStream
   {
      private readonly AzureBlobFileStorage _parent;
      private readonly string _containerName;
      private readonly string _blobName;
      private int _blockId;
      private readonly List<string> _blockIds = new List<string>();

      public AzureWriteStream(AzureBlobFileStorage parent, string containerName, string blobName) : base(1024 * 1024)
      {
         _parent = parent;
         _containerName = containerName;
         _blobName = blobName;
      }

      protected override void DumpBuffer(byte[] buffer, int count, bool isFinal)
      {
         _blockIds.Add(_parent.PutBlock(_blockId++, _containerName, _blobName, buffer, count));
      }

      protected override async Task DumpBufferAsync(byte[] buffer, int count, bool isFinal)
      {
         _blockIds.Add(await _parent.PutBlockAsync(_blockId++, _containerName, _blobName, buffer, count));
      }

      protected override void Commit()
      {
         _parent.PutBlockList(_containerName, _blobName, _blockIds);
      }

      protected override Task CommitAsync()
      {
         return _parent.PutBlockListAsync(_containerName, _blobName, _blockIds);
      }
   }
}
