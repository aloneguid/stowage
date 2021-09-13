using System;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class Run
   {
      [JsonPropertyName("job_id")]
      public long JobId { get; set; }

      [JsonPropertyName("run_id")]
      public long RunId { get; set; }

      [JsonPropertyName("number_in_job")]
      public long NumberInJob { get; set; }

      [JsonPropertyName("state")]
      public RunState State { get; set; }

      [JsonPropertyName("start_time")]
      public long StartTimeEpochMs { get; set; }

      [JsonPropertyName("end_time")]
      public long EndTimeEpochMs { get; set; }

      [JsonPropertyName("execution_duration")]
      public long ExecutionDurationMs { get; set; }

      [JsonPropertyName("run_page_url")]
      public string PageUrl { get; set; }

      public bool IsRunning => State.LifecycleState == "RUNNING" || State.LifecycleState == "TERMINATING";

      public bool IsNotRunning => !IsRunning;

      public bool IsSucceeding => State.ResultState == "SUCCESS" || State.ResultState == "CANCELED";

      public bool IsFailing => !IsSucceeding;
   }
}