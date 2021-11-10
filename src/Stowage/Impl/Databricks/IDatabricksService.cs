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

      Task<string> GetSqlQueryRaw(string queryId);

      Task<SqlQuery> GetSqlQuery(string queryId);

      Task UpdateSqlQueryRaw(string queryId, string rawJson);

      Task<string> CreateSqlQueryRaw(string rawJson);

      Task<IReadOnlyCollection<AclEntry>> GetSqlQueryAcl(string queryId);

      /// <summary>
      /// Rewrites query ACL
      /// </summary>
      /// <param name="queryId"></param>
      /// <param name="acl"></param>
      /// <returns></returns>
      Task SetSqlQueryAcl(string queryId, IEnumerable<AclEntry> acl);


      /// <summary>
      /// Transfer ownership of a query, as described in https://docs.microsoft.com/en-us/azure/databricks/sql/admin/transfer-ownership#--transfer-ownership-of-a-query
      /// </summary>
      /// <param name="queryId"></param>
      /// <param name="newOwnerEmail"></param>
      /// <returns></returns>
      Task TransferQueryOwnership(string queryId, string newOwnerEmail);

      /// <summary>
      /// Gets current user using SCIM API
      /// </summary>
      /// <returns></returns>
      Task<ScimUser> ScimWhoami();

      Task ScimSpList();
   }
}
