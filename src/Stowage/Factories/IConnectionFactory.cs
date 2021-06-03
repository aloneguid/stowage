namespace Stowage.Factories
{
   /// <summary>
   /// Connection factory is responsible for creating storage instances from connection strings. It
   /// is usually implemented by every external module, however is optional.
   /// </summary>
   public interface IConnectionFactory
   {
      /// <summary>
      /// Creates a file storage instance from connection string if possible. When this factory does not support this connection
      /// string it returns null.
      /// </summary>
      IFileStorage Create(ConnectionString connectionString);
   }
}
