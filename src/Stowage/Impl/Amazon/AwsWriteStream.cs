using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stowage.Impl.Amazon
{
   sealed class AwsWriteStream : PolyfilledWriteStream
   {
      private readonly AwsS3FileStorage _parent;
      private readonly string _key;
      private readonly string _uploadId;
      private readonly List<string> _partTags = new List<string>();

      public AwsWriteStream(AwsS3FileStorage parent, string key, string uploadId) : base(1024 * 1024)
      {
         _parent = parent;
         _key = key;
         _uploadId = uploadId;
      }

      protected override void DumpBuffer(byte[] buffer, int count, bool isFinal)
      {
         int partNumber = _partTags.Count + 1;
         string eTag = _parent.UploadPart(_key, _uploadId, partNumber, buffer, count);
         _partTags.Add(eTag);
      }

      protected override async Task DumpBufferAsync(byte[] buffer, int count, bool isFinal)
      {
         int partNumber = _partTags.Count + 1;
         string eTag = await _parent.UploadPartAsync(_key, _uploadId, partNumber, buffer, count);
         _partTags.Add(eTag);
      }


      protected override void Commit()
      {
         _parent.CompleteMultipartUpload(_key, _uploadId, _partTags);
      }

      protected override Task CommitAsync()
      {
         return _parent.CompleteMultipartUploadAsync(_key, _uploadId, _partTags);
      }
   }
}
