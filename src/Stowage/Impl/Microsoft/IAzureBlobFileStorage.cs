using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Microsoft
{
   /// <summary>
   /// Provides MS Azure specific functionality that is very different to common one.
   /// </summary>
   public interface IAzureBlobFileStorage : IFileStorage
   {
      /// <summary>
      /// Creates blob in append mode (Azure Append Blob). The call will fail when trying to append to a non-append blob.
      /// </summary>
      Task<Stream> OpenAppend(IOPath path, CancellationToken cancellationToken = default);
   }
}
