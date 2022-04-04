using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stowage.Impl.Databricks
{
   public interface IDatabricksClient
   {
      Task<IReadOnlyCollection<Job>> ListAllJobs(bool moreDetails);

      Task<Job> LoadJob(long jobId);

      Task RunJobNow(long jobId);

      Task CancelRun(long runId);

      Task<long> CreateJob(string jobJson, string apiVersion="2.0");

      Task ResetJob(long jobId, string jobJson, string apiVersion = "2.0");

      Task<IReadOnlyCollection<ClusterInfo>> ListAllClusters();

      Task<ClusterInfo> LoadCluster(string clusterId);

      Task StartCluster(string clusterId);

      Task RestartCluster(string clusterId);

      Task TerminateCluster(string clusterId);

      Task<IReadOnlyCollection<ClusterEvent>> ListClusterEvents(string clusterId);

      Task<IReadOnlyCollection<ObjectInfo>> WorkspaceLs(IOPath path);

      Task<IReadOnlyCollection<SqlQueryBase>> ListSqlQueries();

      Task<IReadOnlyCollection<SqlDashboardBase>> ListSqlDashboards();

      Task<IReadOnlyCollection<SqlEndpoint>> ListSqlEndpoints();

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

      Task<IReadOnlyCollection<ScimUser>> ScimLsUsers();

      Task ScimSpList();

      Task<string> Exec(string clusterId, Language language, string command, Action<string> progressCallback);
   }
}
