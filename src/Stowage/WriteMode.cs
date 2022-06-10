namespace Stowage
{
   /// <summary>
   /// The way to write
   /// </summary>
   public enum WriteMode
   {
      /// <summary>
      /// Specifies that the system should create a new file. If the file already exists, it will be overwritten.
      /// </summary>
      Create = 0,

      /// <summary>
      /// Specifies that the system should create a new file. If the file already exists, <see cref="System.IO.IOException"/> exception is thrown.
      /// </summary>
      CreateNew = 1,

      /// <summary>
      /// Opens the file if it exists and seeks to the end of the file, or creates a new file.
      /// </summary>
      Append = 2
   }
}
