using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.ProjFS;

namespace Stowage.WinProjFS
{
   internal class StowageProvider : IRequiredCallbacks
   {
      // https://github.com/microsoft/ProjFS-Managed-API/blob/main/simpleProviderManaged/SimpleProvider.cs

      private readonly VirtualizationInstance _vi;
      private readonly IFileStorage _fs;
      private readonly ConcurrentDictionary<Guid, IReadOnlyCollection<IOEntry>> _dirListResults = new ConcurrentDictionary<Guid, IReadOnlyCollection<IOEntry>>();

      // maps relative path to a more full info. Does NOT contain trailing slash for folders
      private readonly ConcurrentDictionary<string, IOEntry> _entryInfo = new ConcurrentDictionary<string, IOEntry>();

      public StowageProvider(IFileStorage fs)
      {
         //string root = Path.Combine(
         //Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
         //"Stowage1");
         string root = "c:\\data\\mapped";

         var notificationMappings = new List<NotificationMapping>();
         _vi = new VirtualizationInstance(root, 0, 0, false, notificationMappings);
         _fs = fs;
      }

      #region [ IRequiredCallbacks ] 

      public HResult StartDirectoryEnumerationCallback(int commandId, Guid enumerationId, string relativePath, uint triggeringProcessId, string triggeringProcessImageFileName)
      {
         Func<Task>? t = async () =>
         {
            try
            {
               IReadOnlyCollection<IOEntry>? ls = await _fs.Ls(relativePath + IOPath.PathSeparatorString, false);

               _dirListResults[enumerationId] = ls;
            }
            catch(Exception ex)
            {
               _dirListResults[enumerationId] = new List<IOEntry>();
            }
         };

         t();

         return HResult.Ok;
      }
      public HResult GetDirectoryEnumerationCallback(int commandId, Guid enumerationId, string filterFileName, bool restartScan, IDirectoryEnumerationResults result)
      {
         if(_dirListResults.TryRemove(enumerationId, out IReadOnlyCollection<IOEntry>? items))
         {
            foreach(IOEntry entry in items)
            {
               string relPath = entry.Path.Full.Trim(IOPath.PathSeparatorChar).Replace(IOPath.PathSeparatorString, "\\");

               result.Add(
                  entry.Name.Trim(IOPath.PathSeparatorChar),
                  entry.Size.HasValue ? entry.Size.Value : 0,
                  entry.Path.IsFolder);

               _entryInfo[relPath] = entry;
            }

            return HResult.Ok;
         }

         return HResult.Pending;
      }

      public HResult EndDirectoryEnumerationCallback(Guid enumerationId)
      {
         return HResult.Ok;
      }

      public HResult GetFileDataCallback(int commandId, string relativePath, ulong byteOffset, uint length, Guid dataStreamId, byte[] contentId, byte[] providerId, uint triggeringProcessId, string triggeringProcessImageFileName)
      {
         return HResult.Ok;
      }
      public HResult GetPlaceholderInfoCallback(int commandId, string relativePath, uint triggeringProcessId, string triggeringProcessImageFileName)
      {
         // todo: check if exists, then return NotFound
         //HResult.FileNotFound

         if(!_entryInfo.TryGetValue(relativePath, out IOEntry? entry))
            return HResult.FileNotFound;

         return _vi.WritePlaceholderInfo(relativePath,
            entry.CreatedTime == null ? DateTime.MinValue : entry.CreatedTime.Value.LocalDateTime,
            DateTime.UtcNow,
            DateTime.UtcNow,
            DateTime.UtcNow,
            FileAttributes.Normal,
            entry.Size == null ? 0 : entry.Size.Value,
            entry.Path.IsFolder,
            new byte[] { 0 }, new byte[] { 1 });
      }

      #endregion

      public void Run()
      {
         _vi.OnQueryFileName = QueryFileNameCallback;

         HResult hr = _vi.StartVirtualizing(this);
      }


      private HResult QueryFileNameCallback(string relativePath)
      {
         HResult hr = HResult.Ok;

         /*string parentDirectory = Path.GetDirectoryName(relativePath);
         string childName = Path.GetFileName(relativePath);

         if(this.GetChildItemsInLayer(parentDirectory).Any(child => Utils.IsFileNameMatch(child.Name, childName)))
         {
            hr = HResult.Ok;
         }
         else
         {
            hr = HResult.FileNotFound;
         }*/

         return hr;
      }
   }
}
