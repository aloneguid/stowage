using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage
{
   abstract class PolyfilledFileStorage : IFileStorage
   {
      public abstract Task<IReadOnlyCollection<IOEntry>> Ls(IOPath path = null, bool recurse = false, CancellationToken cancellationToken = default);

      public abstract Task<Stream> OpenWrite(IOPath path, WriteMode mode, CancellationToken cancellationToken = default);

      public virtual async Task WriteText(IOPath path, string contents, Encoding encoding = null, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         if(contents is null)
            throw new ArgumentNullException(nameof(contents));

         if(!path.IsFile)
            throw new ArgumentException($"{nameof(path)} needs to be a file", nameof(path));

         using Stream ws = await OpenWrite(path, WriteMode.Create);
         Stream rs = contents.ToMemoryStream(encoding ?? Encoding.UTF8);
         await rs.CopyToAsync(ws);
      }

      public async Task WriteAsJson(IOPath path, object value, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));
         if(value is null)
            throw new ArgumentNullException(nameof(value));

         string json = JsonSerializer.Serialize(value);
         await WriteText(path, json);
      }

      public abstract Task<Stream> OpenRead(IOPath path, CancellationToken cancellationToken = default);

      public virtual async Task<string> ReadText(IOPath path, Encoding encoding = null, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         if(!path.IsFile)
            throw new ArgumentException($"{nameof(path)} needs to be a file", nameof(path));

         Stream src = await OpenRead(path, cancellationToken).ConfigureAwait(false);
         if(src == null)
            return null;

         var ms = new MemoryStream();
         using(src)
         {
            await src.CopyToAsync(ms).ConfigureAwait(false);
         }

         return (encoding ?? Encoding.UTF8).GetString(ms.ToArray());

      }

      public async Task<T> ReadAsJson<T>(IOPath path, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         string json = await ReadText(path, null, cancellationToken);
         if(json == null)
            return default;

         return JsonSerializer.Deserialize<T>(json);
      }


      public abstract Task Rm(IOPath path, bool recurse = false, CancellationToken cancellationToken = default);

      protected async Task RmRecurseWithLs(IOPath path, CancellationToken cancellationToken)
      {
         IReadOnlyCollection<IOEntry> entries = await Ls(path.WTS, true);
         foreach(IOEntry entry in entries)
         {
            if(!entry.Path.IsFile)
               continue;

            await Rm(entry.Path, false, cancellationToken);
         }
      }

      public virtual async Task<bool> Exists(IOPath path, CancellationToken cancellationToken = default)
      {
         using(Stream s = await OpenRead(path, cancellationToken))
         {
            return s != null;
         }
      }

      public virtual async Task Ren(IOPath oldPath, IOPath newPath, CancellationToken cancellationToken = default)
      {
         if(oldPath is null)
            throw new ArgumentNullException(nameof(oldPath));
         if(newPath is null)
            throw new ArgumentNullException(nameof(newPath));

         // when file moves to a folder
         if(oldPath.IsFile && newPath.IsFolder)
         {
            newPath = newPath.Combine(oldPath.Name);

            // now it's a file-to-file rename

            throw new NotImplementedException();
         }
         else if(oldPath.IsFolder && newPath.IsFile)
         {
            throw new ArgumentException($"attempted to rename folder to file", nameof(newPath));
         }
         else if(oldPath.IsFolder)
         {
            // folder-to-folder ren
            throw new NotImplementedException();
         }
         else
         {
            // file-to-file ren
            await RenFile(oldPath, newPath, cancellationToken);
         }


      }

      private async Task RenFile(IOPath oldPath, IOPath newPath, CancellationToken cancellationToken = default)
      {
         using(Stream src = await OpenRead(oldPath, cancellationToken))
         {
            if(src != null)
            {
               using(Stream dest = await OpenWrite(newPath, WriteMode.Create, cancellationToken))
               {
                  await src.CopyToAsync(dest);
               }

               await Rm(oldPath);
            }
         }
      }


      public virtual void Dispose()
      {

      }
   }
}
