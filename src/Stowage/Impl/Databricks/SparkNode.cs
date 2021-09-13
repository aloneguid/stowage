using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/clusters#--sparknode
   /// </summary>
   public class SparkNode
   {
      [JsonPropertyName("node_id")]
      public string Id { get; set; }

      [JsonPropertyName("private_id")]
      public string PrivateIp { get; set; }

      [JsonPropertyName("public_dns")]
      public string PublicDns { get; set; }

      [JsonPropertyName("instance_id")]
      public string InstanceId { get; set; }

      [JsonPropertyName("start_timestamp")]
      public long StartTimestamp { get; set; }

      [JsonPropertyName("host_private_ip")]
      public string HostPrivateIp { get; set; }
   }
}
