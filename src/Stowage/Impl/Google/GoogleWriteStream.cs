using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Stowage.Impl.Google
{
   /// <summary>
   /// Implements resumable uploads - https://cloud.google.com/storage/docs/performing-resumable-uploads#initiate-session
   /// </summary>
   sealed class GoogleWriteStream : PolyfilledWriteStream
   {
      private readonly GoogleCloudStorage _parent;
      private readonly string _objectName;
      private string _sessionUri;

      public GoogleWriteStream(GoogleCloudStorage parent, string objectName) : base(1024 * 1024)
      {
         _parent = parent;
         _objectName = objectName;
      }

      protected override void Commit()
      {
         // is there one?
      }

      protected override Task CommitAsync()
      {
         // not required?
         return Task.CompletedTask;
      }

      protected override void DumpBuffer(byte[] buffer, int count, bool isFinal)
      {
         if(_sessionUri == null)
         {
            _sessionUri = _parent.InitiateResumableUpload(_objectName);
         }

         _parent.ResumeUpload(_sessionUri, buffer, count);
      }

      protected override async Task DumpBufferAsync(byte[] buffer, int count, bool isFinal)
      {
         if(_sessionUri == null)
         {
            _sessionUri = await _parent.InitiateResumableUploadAsync(_objectName);
         }

         await _parent.ResumeUploadAsync(_sessionUri, buffer, count);
      }
   }
}
