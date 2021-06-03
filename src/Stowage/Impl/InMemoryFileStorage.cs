using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetBox;
using NetBox.IO;

namespace Stowage.Impl
{
   class InMemoryFileStorage : PolyfilledFileStorage
   {
      private static readonly ConcurrentDictionary<string, InMemoryFileStorage> _idToInstance = new ConcurrentDictionary<string, InMemoryFileStorage>();

      class DataStream : MemoryStream
      {
         private readonly string _path;
         private readonly InMemoryFileStorage _parent;

         public DataStream(string path, InMemoryFileStorage parent)
         {
            _path = IOPath.Normalize(path);
            _parent = parent;
         }

         protected override void Dispose(bool disposing)
         {
            _parent.Add(_path, this);
         }
      }

      struct Tag
      {
         public IOEntry entry;
         public DataStream data;
      }

      private readonly Dictionary<IOPath, Tag> _pathToTag = new Dictionary<IOPath, Tag>();

      public static IFileStorage CreateOrGet(string id)
      {
         id = id ?? "default";

         if(_idToInstance.TryGetValue(id, out InMemoryFileStorage instance))
            return instance;

         instance = new InMemoryFileStorage();
         _idToInstance[id] = instance;
         return instance;
      }

      private InMemoryFileStorage() { }

      public override Task<IReadOnlyCollection<IOEntry>> Ls(IOPath path, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path == null)
            path = IOPath.Root;

         if(!path.IsFolder)
            throw new ArgumentException("path needs to be a folder", nameof(path));

         IEnumerable<KeyValuePair<IOPath, Tag>> query = _pathToTag;

         //limit by folder path
         if(recurse)
         {
            if(!IOPath.IsRoot(path))
            {
               query = query.Where(p => p.Key.Full.StartsWith(path.Full));
            }
         }
         else
         {
            query = query.Where(p => p.Key.Folder == path.Full);
         }

         IReadOnlyCollection<IOEntry> matches = query.Select(p => p.Value.entry).ToList();

         return Task.FromResult(matches);
      }

      public override Task<Stream> OpenWrite(IOPath path, WriteMode mode, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));


         path = IOPath.Normalize(path);

         return Task.FromResult<Stream>(new DataStream(path, this));
      }

      public override Task<Stream> OpenRead(IOPath path, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));


         if(!_pathToTag.TryGetValue(path, out Tag tag) || tag.data == null)
            return Task.FromResult<Stream>(null);

         tag.data.Position = 0;
         return Task.FromResult<Stream>(tag.data);
      }


      public override Task Rm(IOPath path, bool recurse, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         //try to delete as entry
         if(_pathToTag.ContainsKey(path))
         {
            _pathToTag.Remove(path);
         }

         if(path.IsFolder && recurse)
         {
            List<IOPath> candidates = _pathToTag.Where(p => p.Key.Full.StartsWith(path.Full)).Select(p => p.Key).ToList();

            foreach(IOPath candidate in candidates)
            {
               _pathToTag.Remove(candidate);
            }
         }

         return Task.CompletedTask;
      }

      public override void Dispose() {}

      void Add(string path, DataStream sourceStream)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         path = IOPath.Normalize(path);

         if(sourceStream is MemoryStream ms)
            ms.Position = 0;
         byte[] data = sourceStream.ToByteArray();

         if(!_pathToTag.TryGetValue(path, out Tag tag))
         {
            tag = new Tag
            {
               entry = path,
               data = sourceStream,
            };
         }
         else
         {
            tag.entry = path;
            tag.data = sourceStream;
         }
         _pathToTag[path] = tag;

         AddVirtualFolderHierarchy(path);
      }

      private void AddVirtualFolderHierarchy(IOEntry fileBlob)
      {
         string path = fileBlob.Path.Folder;

         while(!IOPath.IsRoot(path))
         {
            var vf = new IOEntry(path + IOPath.PathSeparatorString);
            _pathToTag[path] = new Tag { entry = vf.Path.Full };

            path = IOPath.GetParent(path);
         }
      }

      private bool Exists(string fullPath)
      {
         if(fullPath is null)
            throw new ArgumentNullException(nameof(fullPath));

         return _pathToTag.ContainsKey(fullPath);
      }
   }
}
