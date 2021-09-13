using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/jobs#--job
   /// </summary>
   public class Job
   {
      [JsonPropertyName("job_id")]
      public long Id { get; set; }

      public string Name => Settings.Name;

      [JsonPropertyName("creator_user_name")]
      public string Creator { get; set; }

      [JsonPropertyName("run_as")]
      public string RunAs { get; set; }

      /// <summary>
      /// The time at which this job was created in epoch milliseconds (milliseconds since 1/1/1970 UTC).
      /// </summary>
      [JsonPropertyName("created_time")]
      public long CreatedTimeEpochMs { get; set; }

      [JsonPropertyName("settings")]
      public JobSettings Settings { get; set; }

      public bool IsScheduled => Settings?.Schedule != null;

      public string CronSchedule => Settings?.Schedule?.Expression;

      public string ScheduleDisplay => IsScheduled ? Settings.Schedule.Expression : "None";

      public List<Run> Runs { get; } = new List<Run>();

      public bool RanAtLeastOnce => Runs.Any();

      public Run LastRunningRun => Runs.FirstOrDefault(r => r.IsRunning);

      public Run LastRun => Runs.FirstOrDefault();

      public Run LastFinishedRun => Runs.FirstOrDefault(r => !r.IsRunning);

      public bool IsRunning => LastRunningRun != null && LastRunningRun.IsRunning;

      public bool IsNotRunning => !IsRunning;

   }
}
