using Xunit;

namespace Stowage.Test
{
   public class IOEntryTest
   {
      [Fact]
      public void Is_root_folder_for_root_folder()
      {
         Assert.True(new IOEntry("/").IsRootFolder);
      }

      [Fact]
      public void Is_root_folder_for_non_root_folder()
      {
         Assert.False(new IOEntry("/awesome/").IsRootFolder);
      }
   }
}
