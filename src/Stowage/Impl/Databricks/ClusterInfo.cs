using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/clusters#--clusterinfo
   /// </summary>
   public class ClusterInfo
   {
      [JsonPropertyName("cluster_id")]
      public string Id { get; set; }

      [JsonPropertyName("cluster_name")]
      public string Name { get; set; }

      [JsonPropertyName("cluster_source")]
      public string Source { get; set; }

      /// <summary>
      /// https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/clusters#--clusterstate
      /// </summary>
      [JsonPropertyName("state")]
      public string State { get; set; }

      /// <summary>
      /// A message associated with the most recent state transition (for example, the reason why the cluster entered a TERMINATED state).
      /// </summary>
      [JsonPropertyName("state_message")]
      public string StateMessage { get; set; }

      public bool IsRunning => State == "RUNNING" || State == "RESIZING";

      public bool IsNotRunning => !IsRunning;

      /// <summary>
      /// If num_workers, number of worker nodes that this cluster should have.
      /// A cluster has one Spark driver and num_workers executors for a total of num_workers + 1 Spark nodes.
      /// </summary>
      [JsonPropertyName("num_workers ")]
      public int NumWorkers { get; set; }

      /// <summary>
      /// If autoscale, parameters needed in order to automatically scale clusters up and down based on load.
      /// </summary>
      [JsonPropertyName("autoscale")]
      public AutoScale AutoScale { get; set; }

      public bool IsAutoscaling => AutoScale != null;

      public bool IsStaticSize => !IsAutoscaling;

      /// <summary>
      /// Creator user name. The field won’t be included in the response if the user has already been deleted.
      /// </summary>
      [JsonPropertyName("creator_user_name")]
      public string Creator { get; set; }

      /// <summary>
      /// The runtime version of the cluster. You can retrieve a list of available runtime versions by using the Runtime versions API call.
      /// </summary>
      [JsonPropertyName("spark_version")]
      public string SparkVersion { get; set; }

      [JsonPropertyName("spark_conf")]
      public Dictionary<string, string> SparkConf { get; set; }

      public string SparkConfAsString => SparkConf == null
         ? null
         : string.Join(Environment.NewLine, SparkConf.Select(i => $"{i.Key} {i.Value}"));

      [JsonPropertyName("node_type_id")]
      public string NodeTypeId { get; set; }

      /// <summary>
      /// The node type of the Spark driver.
      /// This field is optional; if unset, the driver node type will be set as the same value as node_type_id defined above.
      /// </summary>
      [JsonPropertyName("driver_node_type_id")]
      public string DriverNodeTypeId { get; set; }

      public string DriverNodeTypeIdDisplay => DriverNodeTypeId ?? NodeTypeId;

      [JsonPropertyName("spark_env_vars")]
      public Dictionary<string, string> SparkEnvVars { get; set; }

      public string SparkEnvVarsAsString => SparkEnvVars == null
         ? null
         : string.Join(Environment.NewLine, SparkEnvVars.Select(i => $"{i.Key} {i.Value}"));


      [JsonPropertyName("start_time")]
      public long StartTimeEpochMs { get; set; }

      [JsonPropertyName("terminated_time")]
      public long TerminatedTimeEpochMs { get; set; }

      [JsonPropertyName("last_activity_time")]
      public long LastActivityTimeEpochMs { get; set; }

      [JsonPropertyName("driver")]
      public SparkNode Driver { get; set; }

      [JsonPropertyName("executors")]
      public SparkNode[] Executors { get; set; }

      public int ExecutorCount => Executors?.Length ?? 0;

      public int NodeCount => 1 + ExecutorCount;

      [JsonPropertyName("jdbc_port")]
      public int JdbcPort { get; set; }

      [JsonPropertyName("cluster_memory_mb")]
      public long ClusterMemoryMb { get; set; }

      [JsonPropertyName("cluster_cores")]
      public float ClusterCores { get; set; }

      [JsonPropertyName("autotermination_minutes")]
      public long AutoterminationMinutes { get; set; }

      public bool CanAutoterminate => AutoterminationMinutes > 0;
   }
}
