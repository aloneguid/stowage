using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Stowage
{
   // https://github.com/aloneguid/logmagic/blob/master/src/LogMagic/LogContext.cs
   public static class IOContext
   {
      private static readonly AsyncLocal<IFileStorage> IFS = new AsyncLocal<IFileStorage>();

      public static IDisposable Push(IFileStorage ifs)
      {
         var bookmark = new StackBookmark(IFS.Value);
         IFS.Value = ifs;
         return bookmark;
      }

      public static IFileStorage Storage => IFS.Value;

      sealed class StackBookmark : IDisposable
      {
         private readonly IFileStorage _ifs;

         public StackBookmark(IFileStorage ifs)
         {
            _ifs = ifs;
         }

         public void Dispose()
         {
            IFS.Value = _ifs;
         }
      }
   }


}
