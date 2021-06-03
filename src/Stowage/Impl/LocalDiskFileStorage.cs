using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SysIO = System.IO;

namespace Stowage.Impl
{
   class LocalDiskFileStorage : PolyfilledFileStorage
   {
      private readonly string _directoryFullName;

      /// <summary>
      /// Creates an instance in a specific disk directory
      /// <param name="directoryFullName">Root directory</param>
      /// </summary>
      public LocalDiskFileStorage(string directoryFullName)
      {
         if(directoryFullName == null)
            throw new ArgumentNullException(nameof(directoryFullName));

         _directoryFullName = Path.GetFullPath(directoryFullName);
      }

      private IReadOnlyCollection<IOEntry> List(string path, bool recurse, bool addAttributes)
      {
         string lPath = GetFilePath(path, false);

         if(Directory.Exists(lPath))
         {
            var di = new DirectoryInfo(lPath);

            FileSystemInfo[] fInfos = di.GetFileSystemInfos("*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            return fInfos.Select(i => ToIOEntry(i, addAttributes)).ToList();
         }

         return new IOEntry[0];
      }

      public override Task<IReadOnlyCollection<IOEntry>> Ls(IOPath path = null, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path != null && !path.IsFolder)
            throw new ArgumentException("path needs to be a folder", nameof(path));

         return Task.FromResult(List(path, recurse, true));
      }

      private IOEntry ToIOEntry(FileSystemInfo info, bool addAttributes)
      {
         string relPath = info.FullName.Substring(_directoryFullName.Length);
         relPath = relPath.Replace(Path.DirectorySeparatorChar, IOPath.PathSeparator);
         relPath = relPath.Trim(IOPath.PathSeparator);
         relPath = IOPath.PathSeparatorString + relPath;

         bool isFolder = info is DirectoryInfo;
         if(isFolder)
         {
            relPath += IOPath.PathSeparatorString;
         }

         var entry = new IOEntry(relPath);

         if(addAttributes)
         {
            entry.CreatedTime = info.CreationTimeUtc;
            entry.LastModificationTime = info.LastWriteTimeUtc;

            entry.TryAddProperties(
               "LastAccessTimeUtc", info.LastAccessTimeUtc,
               "Attributes", info.Attributes);

            if(!isFolder)
            {
               var fi = (FileInfo)info;
               entry.Size = fi.Length;
            }
         }

         return entry;
      }


      public override Task<Stream> OpenWrite(IOPath path, WriteMode mode, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         string npath = IOPath.Normalize(path);

         return Task.FromResult(CreateStream(npath, mode != WriteMode.Append));
      }

      public override Task<Stream> OpenRead(IOPath path, CancellationToken cancellationToken)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         Stream result = OpenStream(path);

         return Task.FromResult(result);
      }


      public override Task Rm(IOPath path, bool recurse, CancellationToken cancellationToken = default)
      {
         if(path == null)
            throw new ArgumentNullException(nameof(path));

         string localPath = GetFilePath(path, false);

         if(File.Exists(localPath))
         {
            File.Delete(localPath);
         }
         else if(Directory.Exists(localPath))
         {
            Directory.Delete(localPath, true);
         }

         return Task.CompletedTask;
      }

      private static string EncodePathPart(string path)
      {
         return path;
      }

      private Stream OpenStream(string path)
      {
         path = GetFilePath(path);

         if(!SysIO.File.Exists(path))
            return null;

         return SysIO.File.OpenRead(path);
      }


      private string GetFilePath(IOPath path, bool createIfNotExists = true)
      {
         if(path.IsRootPath)
            return _directoryFullName;

         //id can contain path separators
         string rawPath = path.Full.Trim(IOPath.PathSeparator);
         string[] parts = rawPath.Split(IOPath.PathSeparator).Select(EncodePathPart).ToArray();
         string name = parts[parts.Length - 1];
         string dir;
         if(parts.Length == 1)
         {
            dir = _directoryFullName;
         }
         else
         {
            string extraPath = string.Join(IOPath.PathSeparatorString, parts, 0, parts.Length - 1);

            rawPath = Path.Combine(_directoryFullName, extraPath);

            dir = rawPath;
            if(!Directory.Exists(dir))
               Directory.CreateDirectory(dir);
         }

         return Path.Combine(dir, name);
      }

      private Stream CreateStream(string fullPath, bool overwrite = true)
      {
         if(!SysIO.Directory.Exists(_directoryFullName))
            SysIO.Directory.CreateDirectory(_directoryFullName);
         string path = GetFilePath(fullPath);

         SysIO.Directory.CreateDirectory(Path.GetDirectoryName(path));
         Stream s = overwrite ? SysIO.File.Create(path) : SysIO.File.OpenWrite(path);
         s.Seek(0, SeekOrigin.End);
         return s;
      }
   }
}
