using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

         //IFileStorage dbc1 = Files.Of.DatabricksDbfsFromLocalProfile();
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
      public async Task DashLs()
      {
         IReadOnlyCollection<SqlDashboardBase> dashboards = await dbc.ListSqlDashboards();
      }

      [Fact]
      public async Task TakeOwnership()
      {
         IReadOnlyCollection<SqlQueryBase> queries = await dbc.ListSqlQueries();

         IReadOnlyCollection<AclEntry> acl = await dbc.GetAcl(SqlObjectType.Query, queries.First().Id);

         // keep only the first entry (for test purposes)

         

      }

      const string JobJson = @"{
   ""name"": ""my first job"",
  ""new_cluster"": {
    ""spark_version"": ""7.3.x-scala2.12"",
    ""node_type_id"": ""Standard_D3_v2"",
    ""num_workers"": 0,
    ""custom_tags"": {
      ""ResourceClass"": ""SingleNode""
    }
  }
}
";

      [Fact]
      public async Task CreateAndUpdateJob()
      {
         long jobId = await dbc.CreateJob(JobJson);
         Assert.True(jobId > 0);

         await dbc.ResetJob(jobId, JobJson.Replace("my first job", "my second job"));
      }

      [Fact]
      public async Task Scim()
      {
         await dbc.ScimSpList();

         ScimUser me = await dbc.ScimWhoami();

         IReadOnlyCollection<ScimUser> users = await dbc.ScimLsUsers();
      }

      [Fact]
      public async Task Exec()
      {
         //get first cluster
         ClusterInfo cluster = (await dbc.ListAllClusters()).First();

         // exec simple command
         await dbc.Exec(cluster.Id, Language.Sql, "show databases", Console.WriteLine);
      }
   }
}
