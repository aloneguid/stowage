﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stowage.Impl.Databricks;
using Xunit;

namespace Stowage.Test.Integration.Impl {
    [Trait("Category", "Integration")]
    public class DatabricksTest {
        private readonly IDatabricksClient dbc;

        public DatabricksTest() {
            ITestSettings settings = ConfigLoader.Load();

            dbc = (IDatabricksClient)Files.Of.DatabricksDbfs(settings.DatabricksBaseUri, settings.DatabricksToken);
        }

        [Fact]
        public async Task ListClusters() {
            IReadOnlyCollection<ClusterInfo> clusters = await dbc.LsClusters();

            Assert.NotEmpty(clusters);
        }

        [Fact]
        public async Task WorkspaceLs() {
            IReadOnlyCollection<ObjectInfo> objs = await dbc.LsWorkspace("/");

            Assert.NotEmpty(objs);

        }

        [Fact]
        public async Task SqlLs() {
            IReadOnlyCollection<DataSource> dss = await dbc.LsDataSources();

            IReadOnlyCollection<SqlQuery> queries = await dbc.LsSqlQueries();

            IReadOnlyCollection<SqlDashboard> dashboards = await dbc.LsSqlDashboards();

            string dashboardRaw = await dbc.GetSqlDashboardRaw(dashboards.First().Id);

            Assert.NotEmpty(queries);
        }

        [Fact]
        public async Task TakeOwnership() {
            IReadOnlyCollection<SqlQuery> queries = await dbc.LsSqlQueries();

            string qs = await dbc.GetSqlQueryRaw(queries.First().Id);
            SqlQuery q = await dbc.GetSqlQuery(queries.First().Id);

            IReadOnlyCollection<AclEntry> acl = await dbc.GetAcl(SqlObjectType.Query, queries.First().Id);

            // keep only the first entry (for test purposes)

            IReadOnlyCollection<SqlEndpoint> endpoints = await dbc.LsSqlEndpoints();


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
        public async Task CreateAndUpdateJob() {
            long jobId = await dbc.CreateJob(JobJson);
            Assert.True(jobId > 0);

            await dbc.ResetJob(jobId, JobJson.Replace("my first job", "my second job"));
        }

        [Fact]
        public async Task Scim() {
            await dbc.LsScimSp();

            ScimUser me = await dbc.ScimWhoami();

            IReadOnlyCollection<ScimUser> users = await dbc.LsScimUsers();
        }

        [Fact]
        public async Task Exec() {
            //get first cluster
            ClusterInfo cluster = (await dbc.LsClusters()).First();

            // exec simple command
            await dbc.Exec(cluster.Id, Language.Sql, "show databases", Console.WriteLine);
        }
    }
}
