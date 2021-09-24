using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Config.Net;
using Stowage.Impl.Databricks;
using Xunit;

namespace Stowage.Test.Impl
{
   [Trait("Category", "Integration")]
   public class DatabricksTest
   {
      private readonly IDatabricksClient dbc;

      public DatabricksTest()
      {
         ITestSettings settings = new ConfigurationBuilder<ITestSettings>()
            .UseIniFile("c:\\tmp\\integration-tests.ini")
            .Build();

         dbc = (IDatabricksClient)Files.Of.DatabricksDbfs(settings.DatabricksBaseUri, settings.DatabricksToken);
      }

      [Fact]
      public async Task ListClusters()
      {
         IReadOnlyCollection<ClusterInfo> clusters = await dbc.ListAllClusters();

         Assert.NotEmpty(clusters);
      }

      [Fact]
      public async Task WorkspaceLs()
      {
         IReadOnlyCollection<ObjectInfo> objs = await dbc.WorkspaceLs("/");

         Assert.NotEmpty(objs);

      }

      [Fact]
      public async Task SqlLs()
      {
         IReadOnlyCollection<SqlQueryBase> queries = await dbc.ListSqlQueries();

         Assert.NotEmpty(queries);
      }

      [Fact]
      public async Task TakeOwnership()
      {
         await dbc.TransferQueryOwnership("...", "...");
      }
   }
}
