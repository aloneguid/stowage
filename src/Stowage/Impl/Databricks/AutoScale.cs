using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/clusters#--autoscale
   /// </summary>
   public class AutoScale
   {
      [JsonPropertyName("min_workers")]
      public int Min { get; set; }

      [JsonPropertyName("max_workers")]
      public int Max { get; set; }
   }
}
