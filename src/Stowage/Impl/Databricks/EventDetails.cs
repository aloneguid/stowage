using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/clusters#--eventdetails
   /// </summary>
   public class EventDetails
   {
      [JsonPropertyName("current_num_workers")]
      public int CurrentNumWorkers { get; set; }

      [JsonPropertyName("target_num_workers")]
      public int TargetNumWorkers { get; set; }

      [JsonPropertyName("cause")]
      public string Cause { get; set; }

      public bool HasCause => Cause != null;

      [JsonPropertyName("user")]
      public string User { get; set; }

      public bool HasUser => User != null;
   }
}
