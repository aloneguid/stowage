using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class CronSchedule
   {
      [JsonPropertyName("quartz_cron_expression")]
      public string Expression { get; set; }

      [JsonPropertyName("timezone_id")]
      public string TimezoneId { get; set; }

      [JsonPropertyName("pause_status")]
      public string PauseStatus { get; set; }
   }
}
