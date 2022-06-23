using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// Expresses schedule
   /// </summary>
   public class QueryInterval
   {
      /// <summary>
      /// For weekly runs, a day of the week for run to start.
      /// </summary>
      /// <example>Wednesday</example>
      [JsonPropertyName("day_of_week")]
      public string DayOfWeek { get; set; }

      /// <summary>
      /// Integer number of seconds between runs.
      /// </summary>
      /// <example>900</example>
      [JsonPropertyName("interval")]
      public int Interval { get; set; }

      /// <summary>
      /// For daily, weekly, and monthly runs, the time-of-day for run to start.
      /// </summary>
      /// <example>00:15</example>
      [JsonPropertyName("time")]
      public string Time { get; set; }

      /// <summary>
      /// A date after which this schedule no longer applies.
      /// </summary>
      /// <example>2021-01-01</example>
      [JsonPropertyName("until")]
      public string Until { get; set; }
   }
}
