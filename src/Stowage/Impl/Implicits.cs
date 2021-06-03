using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stowage.Impl
{
   static class Implicits
   {
      public static void AssumeImplicitFolders(string absoluteRoot, List<IOEntry> entries)
      {
         absoluteRoot = IOPath.Normalize(absoluteRoot);

         List<IOEntry> implicitFolders = entries
            .Select(b => b.Path.Full)
            .Select(p => p.Substring(absoluteRoot.Length))
            .Select(p => IOPath.GetParent(p))
            .Where(p => !IOPath.IsRoot(p))
            .Distinct()
            .Select(p => new IOEntry(p + "/"))
            .ToList();

         entries.InsertRange(0, implicitFolders);
      }
   }
}
