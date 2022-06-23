using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// Extra functionality specific to databricks
   /// </summary>
   public interface IDatabricksClient
   {
      #region [ Jobs ]

      /// <summary>
      /// Lists jobs
      /// </summary>
      Task<IReadOnlyCollection<Job>> LsJobs(bool moreDetails);

      /// <summary>
      /// Gets job detail by ID
      /// </summary>
      Task<Job> GetJob(long jobId);

      Task RunJobNow(long jobId);

      Task CancelJobRun(long runId);

      Task<long> CreateJob(string jobJson, string apiVersion="2.0");

      Task ResetJob(long jobId, string jobJson, string apiVersion = "2.0");

      #endregion

      #region [ Clusters ]

      Task<IReadOnlyCollection<ClusterInfo>> LsClusters();

      Task<ClusterInfo> LoadCluster(string clusterId);

      Task StartCluster(string clusterId);

      Task RestartCluster(string clusterId);

      Task TerminateCluster(string clusterId);

      Task<IReadOnlyCollection<ClusterEvent>> LsClusterEvents(string clusterId);

      #endregion

      #region [ Workspace ]

      Task<IReadOnlyCollection<ObjectInfo>> LsWorkspace(IOPath path);

      #endregion

      #region [ SQL Queries ]

      Task<IReadOnlyCollection<SqlQuery>> LsSqlQueries(Func<long, long, Task> progress = null);

      Task<string> GetSqlQueryRaw(string queryId);

      Task<SqlQuery> GetSqlQuery(string queryId);

      Task UpdateSqlQueryRaw(string queryId, string rawJson);

      Task<string> CreateSqlQueryRaw(string rawJson);

      /// <summary>
      /// Moves query to trash (deleted after 30 days)
      /// </summary>
      Task DeleteSqlQuery(string queryId);

      #endregion

      #region [ Dashboards ]

      Task<IReadOnlyCollection<SqlDashboardBase>> LsSqlDashboards();


      #endregion


      #region [ Endpoints ]

      Task<IReadOnlyCollection<DataSource>> LsDataSources();

      Task<IReadOnlyCollection<SqlEndpoint>> LsSqlEndpoints();

      #endregion

      #region [ SCIM ]

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

      #endregion

      #region [ Remote Execution ]

      Task<string> Exec(string clusterId, Language language, string command, Action<string> progressCallback);

      #endregion

      #region [ Security ]

      Task<IReadOnlyCollection<AclEntry>> GetAcl(SqlObjectType objectType, string objectId);

      Task SetAcl(SqlObjectType objectType, string objectId, IEnumerable<AclEntry> acl);


      #endregion
   }
}
