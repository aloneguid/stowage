using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Stowage.Impl.Databricks
{
   public class SqlEndpointChannel
   {
      [JsonPropertyName("name")]
      public string Name { get; set; }
   }

   public class SqlEndpointHealth
   {
      [JsonPropertyName("status")]
      public string Status { get; set; }
   }

   public class SqlEndpoint
   {
      [JsonPropertyName("id")]
      public string Id { get; set; }

      [JsonPropertyName("name")]
      public string Name { get; set; }

      [JsonPropertyName("size")]
      public string Size { get; set; }

      [JsonPropertyName("cluster_size")]
      public string ClusterSize { get; set; }

      [JsonPropertyName("min_num_clusters")]
      public int MinClusters { get; set; }

      [JsonPropertyName("max_num_clusters")]
      public int MaxClusters { get; set; }

      [JsonPropertyName("auto_stop_mins")]
      public int AutoStopMins { get; set; }

      [JsonPropertyName("auto_resume")]
      public bool AutoResume { get; set; }

      [JsonPropertyName("creator_name")]
      public string CreatedBy { get; set; }

      [JsonPropertyName("spot_instance_policy")]
      public string SpotInstancePolicy { get; set; }

      [JsonPropertyName("enable_photon")]
      public bool PhotonEnabled { get; set; }

      [JsonPropertyName("channel")]
      public SqlEndpointChannel Channel { get; set; }

      [JsonPropertyName("enable_serverless_compute")]
      public bool ServerlessComputeEnabled { get; set; }

      [JsonPropertyName("num_clusters")]
      public int NumClusters { get; set; }

      [JsonPropertyName("num_active_sessions")]
      public int NumActionSessions { get; set; }

      [JsonPropertyName("state")]
      public string State { get; set; }

      [JsonPropertyName("jdbc_url")]
      public string JdbcUrl { get; set; }

      [JsonPropertyName("health")]
      public SqlEndpointHealth Health { get; set; }

   }
}
