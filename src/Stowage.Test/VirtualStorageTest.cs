using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Stowage.Test
{
   public class VirtualStorageTest
   {
      private readonly IVirtualStorage _vfs = Files.Of.VFS();
      private readonly IFileStorage _drivec = Files.Of.InternalMemory(Guid.NewGuid().ToString());
      private readonly IFileStorage _films = Files.Of.InternalMemory(Guid.NewGuid().ToString());

      public VirtualStorageTest()
      {
         _vfs.WriteText("/1.txt", "1.txt");
         _vfs.WriteText("/mnt/readme.txt", "this folder contains mounts");

         _drivec.WriteText("/users/ivan/desktop.ini", "not sure");
         _films.WriteText("/animated/up.mkv", "play");
         _films.WriteText("/1984/stealthisfilm.mkv", "play");

         _vfs.Mount("/mnt/c/", _drivec);
         _vfs.Mount("/mnt/films/", _films);
      }

      [Fact]
      public void Mount_NonFolder_ArgNull()
      {
         Assert.Throws<ArgumentException>(() => _vfs.Mount("/mnt/file", _drivec));
      }

      [Fact]
      public async Task List_Root_FileAndMnt()
      {
         IReadOnlyCollection<IOEntry> root = await _vfs.Ls();

         Assert.Equal(2, root.Count);
      }

      [Fact]
      public async Task List_InMnt_Mounts()
      {
         IReadOnlyCollection<IOEntry> mnts = await _vfs.Ls("/mnt/");

         Assert.Equal(3, mnts.Count);
         Assert.Equal(new[] { "/mnt/readme.txt", "/mnt/c/", "/mnt/films/" }, mnts.Select(m => m.Path.Full));
      }

      [Fact]
      public async Task List_InFilmsRoot_TwoFolders()
      {
         IReadOnlyCollection<IOEntry> r = await _vfs.Ls("mnt/films/");

         Assert.Equal(2, r.Count);
         Assert.Equal(new[] { "/mnt/films/animated/", "/mnt/films/1984/" }, r.Select(r => r.Path.Full));
      }

   }
}
