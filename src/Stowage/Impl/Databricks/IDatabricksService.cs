using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stowage.Impl.Databricks
{
   public interface IDatabricksClient
   {
      Task<IReadOnlyCollection<Job>> LsJobs(bool moreDetails);

      Task<Job> GetJob(long jobId);

      Task RunJobNow(long jobId);

      Task CancelJobRun(long runId);

      Task<long> CreateJob(string jobJson, string apiVersion="2.0");

      Task ResetJob(long jobId, string jobJson, string apiVersion = "2.0");

      Task<IReadOnlyCollection<ClusterInfo>> LsClusters();

      Task<ClusterInfo> LoadCluster(string clusterId);

      Task StartCluster(string clusterId);

      Task RestartCluster(string clusterId);

      Task TerminateCluster(string clusterId);

      Task<IReadOnlyCollection<ClusterEvent>> LsClusterEvents(string clusterId);

      Task<IReadOnlyCollection<ObjectInfo>> LsWorkspace(IOPath path);

      Task<IReadOnlyCollection<SqlQuery>> LsSqlQueries(Func<long, long, Task> progress = null);

      Task<IReadOnlyCollection<SqlDashboardBase>> LsSqlDashboards();

      Task<IReadOnlyCollection<SqlEndpoint>> LsSqlEndpoints();

      Task<string> GetSqlQueryRaw(string queryId);

      Task<SqlQuery> GetSqlQuery(string queryId);

      Task UpdateSqlQueryRaw(string queryId, string rawJson);

      Task<string> CreateSqlQueryRaw(string rawJson);

      Task<IReadOnlyCollection<AclEntry>> GetAcl(SqlObjectType objectType, string objectId);

      Task SetAcl(SqlObjectType objectType, string objectId, IEnumerable<AclEntry> acl);

      /// <summary>
      /// Gets current user using SCIM API
      /// </summary>
      /// <returns></returns>
      Task<ScimUser> ScimWhoami();

      Task<IReadOnlyCollection<ScimUser>> LsScimUsers();

      /// <summary>
      /// List Service Principals
      /// </summary>
      /// <returns></returns>
      Task LsScimSp();

      Task<string> Exec(string clusterId, Language language, string command, Action<string> progressCallback);
   }
}
