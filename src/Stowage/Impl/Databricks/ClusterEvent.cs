using System;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/clusters#--clusterevent
   /// </summary>
   public class ClusterEvent
   {
      [JsonPropertyName("timestamp")]
      public long TimestampEpochMs { get; set; }

      [JsonPropertyName("type")]
      public string Type { get; set; }

      [JsonPropertyName("details")]
      public EventDetails Details { get; set; }

      //public bool HasDetails => Details != null;
   }
}
