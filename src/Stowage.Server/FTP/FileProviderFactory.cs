using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Stowage;
using Zhaobang.FtpServer.File;

namespace System.IO.Files.Server.FTP
{
   class FileProviderFactory : IFileProviderFactory
   {
      private readonly string _cs;

      public FileProviderFactory(string cs)
      {
         _cs = cs;
      }

      public IFileProvider GetProvider(string user)
      {
         return new FileProvider(_cs);
      }
   }

   class FileProvider : IFileProvider
   {
      private readonly IFileStorage _fs;
      private IOPath _wd = IOPath.Root;

      public FileProvider(string cs)
      {
         _fs = Stowage.Files.Of.ConnectionString(cs);
      }

      public Task CreateDirectoryAsync(string path)
      {
         Log.Information("creating directory {0}", path);
         throw new NotImplementedException();
      }

      public Task<Stream> CreateFileForWriteAsync(string relPath)
      {
         IOPath absPath = IOPath.Combine(_wd, relPath);
         Log.Information("creating file, rel: {0}, abs: {1}", relPath, absPath);
         return _fs.OpenWrite(absPath, WriteMode.Create);
      }

      public async Task DeleteAsync(string relPath)
      {
         IOPath absPath = IOPath.Combine(_wd, relPath);
         Log.Information("deleting file {0} ({1})", relPath, absPath);
         await _fs.Rm(absPath);
      }

      public async Task DeleteDirectoryAsync(string relPath)
      {
         IOPath absPath = IOPath.Combine(_wd, relPath);
         Log.Information("deleting directory {0} ({1})", relPath, absPath);
         await _fs.Rm(absPath, true);
      }

      public async Task<IEnumerable<FileSystemEntry>> GetListingAsync(string path)
      {
         Log.Information("started listing {0}", _wd);
         IReadOnlyCollection<IOEntry> entries = await _fs.Ls(_wd);
         Log.Information("retreived {0} entri(es).", entries.Count);
         return entries.Select(ToFSE);

      }
      public Task<IEnumerable<string>> GetNameListingAsync(string path)
      {
         Log.Information("getnamelisting {0}", path);
         throw new NotImplementedException();
      }

      public string GetWorkingDirectory()
      {
         Log.Information("requested wd {0}", _wd);
         return _wd;
      }

      public async Task<Stream> OpenFileForReadAsync(string relPath)
      {
         // https://github.com/taoyouh/FtpServer/blob/0eda8c026e9851f251612797b460fdc5faf0f4f7/Library/File/SimpleFileProvider.cs#L144
         IOPath absPath = IOPath.Combine(_wd, relPath);
         Log.Information("opening {0} ({1})", relPath, absPath);
         Stream s = await _fs.OpenRead(absPath);
         Log.Information("open, length: {0}", s.Length);
         return s;
      }

      public Task<Stream> OpenFileForWriteAsync(string path)
      {
         Log.Information("opening {0} for writing", path);
         throw new NotImplementedException();
      }

      public Task RenameAsync(string fromPath, string toPath)
      {
         Log.Information("renaming {0} to {1}", fromPath, toPath);
         throw new NotImplementedException();
      }

      public bool SetWorkingDirectory(string path)
      {
         _wd = path;
         Log.Information("home dir set to {0}", _wd);
         return true;
      }

      private FileSystemEntry ToFSE(IOEntry i)
      {
         var r = new FileSystemEntry();
         r.Name = i.Name;
         r.IsDirectory = i.Path.IsFolder;
         r.IsReadOnly = false;
         r.Length = i.Size ?? 0;
         return r;
      }
   }
}
