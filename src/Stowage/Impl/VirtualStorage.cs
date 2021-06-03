using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl
{
   sealed class VirtualStorage : IVirtualStorage
   {
      private class MountPoint
      {
         public IOPath Path;
         public IFileStorage Storage;
      }

      private readonly List<MountPoint> _mps = new List<MountPoint>();
      private readonly IFileStorage _rootFs;

      public VirtualStorage(IFileStorage rootFs = null)
      {
         _rootFs = rootFs ?? InMemoryFileStorage.CreateOrGet(Guid.NewGuid().ToString());
      }

      public void Mount(IOPath path, IFileStorage storage)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));
         if(storage is null)
            throw new ArgumentNullException(nameof(storage));
         if(!path.IsFolder)
            throw new ArgumentException($"only folders can be mounted", nameof(path));

         _mps.Add(new MountPoint { Path = path, Storage = storage });
      }

      public async Task<IReadOnlyCollection<IOEntry>> Ls(IOPath path = null, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path == null)
            path = IOPath.Root;

         // do a usual LS for the root FS
         List<IOEntry> result = (await _rootFs.Ls(path, recurse, cancellationToken)).ToList();

         foreach(MountPoint mp in _mps)
         {
            // if mount point is at this locaiton
            if(mp.Path.Parent.Equals(path))
            {
               result.Add(new IOEntry(mp.Path));
            }

            // ?
            if(path.Full.StartsWith(mp.Path.Full))
            {
               IOPath mpRelPath = new IOPath(path.Full.Substring(mp.Path.Full.Length));

               IReadOnlyCollection<IOEntry> more = await mp.Storage.Ls(mpRelPath, recurse, cancellationToken);

               result.AddRange(more.Select(i => new IOEntry(mp.Path.Combine(i.Path.Full))));
            }
         }

         return result;
      }

      public Task<bool> Exists(IOPath path, CancellationToken cancellationToken = default) => throw new NotImplementedException();
      public Task<Stream> OpenRead(IOPath path, CancellationToken cancellationToken = default) => throw new NotImplementedException();
      public Task<Stream> OpenWrite(IOPath path, WriteMode mode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
      public Task<string> ReadText(IOPath path, Encoding encoding = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
      public Task Rm(IOPath path, bool recurse = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();

      public async Task WriteText(IOPath path, string contents, Encoding encoding = null, CancellationToken cancellationToken = default)
      {
         await _rootFs.WriteText(path, contents, encoding, cancellationToken);
      }


      public void Dispose()
      {

      }

      public Task Ren(IOPath name, IOPath newName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
   }
}
