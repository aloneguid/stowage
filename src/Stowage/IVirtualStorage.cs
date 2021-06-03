namespace Stowage
{
   /// <summary>
   /// Virtual storage. Possibly rename to FilesystemStoreage, as it's essentially a virtual filesystem.
   /// </summary>
   public interface IVirtualStorage : IFileStorage
   {
      /// <summary>
      /// Mounts a storage to virtual path
      /// </summary>
      void Mount(IOPath path, IFileStorage storage);
   }
}
