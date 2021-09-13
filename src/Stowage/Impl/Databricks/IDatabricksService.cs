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

      Task<IReadOnlyCollection<ClusterInfo>> ListAllClusters();

      Task<ClusterInfo> LoadCluster(string clusterId);

      Task StartCluster(string clusterId);

      Task RestartCluster(string clusterId);

      Task TerminateCluster(string clusterId);

      Task<IReadOnlyCollection<ClusterEvent>> ListClusterEvents(string clusterId);

      Task<IReadOnlyCollection<ObjectInfo>> WorkspaceLs(IOPath path);

      Task<IReadOnlyCollection<SqlQuery>> ListSqlQueries();
   }
}
