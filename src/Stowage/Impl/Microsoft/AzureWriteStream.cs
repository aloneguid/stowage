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
      private readonly bool _append;
      private int _blockId;
      private readonly List<string> _blockIds = new List<string>();

      public AzureWriteStream(AzureBlobFileStorage parent, string containerName, string blobName, bool append) : base(1024 * 1024)
      {
         _parent = parent;
         _containerName = containerName;
         _blobName = blobName;
         _append = append;
      }

      protected override void DumpBuffer(byte[] buffer, int count, bool isFinal)
      {
         if(_append)
         {
            try
            {
               _parent.AppendBlock(_containerName, _blobName, buffer, count);
            }
            catch(FileNotFoundException)
            {
               _parent.PutBlob(_containerName, _blobName, "AppendBlob");
               _parent.AppendBlock(_containerName, _blobName, buffer, count);
            }
         }
         else
         {
            _blockIds.Add(_parent.PutBlock(_blockId++, _containerName, _blobName, buffer, count));

         }
      }

      protected override async Task DumpBufferAsync(byte[] buffer, int count, bool isFinal)
      {
         if(_append)
         {
            try
            {
               await _parent.AppendBlockAsync(_containerName, _blobName, buffer, count);
            }
            catch(FileNotFoundException)
            {
               await _parent.PutBlobAsync(_containerName, _blobName, "AppendBlob");
               await _parent.AppendBlockAsync(_containerName, _blobName, buffer, count);
            }
         }
         else
         {
            _blockIds.Add(await _parent.PutBlockAsync(_blockId++, _containerName, _blobName, buffer, count));

         }
      }

      protected override void Commit()
      {
         // no need to commit append blobs
         if(!_append)
         {
            _parent.PutBlockList(_containerName, _blobName, _blockIds);
         }
      }

      protected override Task CommitAsync()
      {
         // no need to commit append blobs
         if(!_append)
         {
            return _parent.PutBlockListAsync(_containerName, _blobName, _blockIds);
         }

         return Task.CompletedTask;
      }
   }
}
