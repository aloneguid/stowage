using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class RunState
   {
      [JsonPropertyName("life_cycle_state")]
      public string LifecycleState { get; set; }

      [JsonPropertyName("result_state")]
      public string ResultState { get; set; }

      [JsonPropertyName("state_message")]
      public string StateMessage { get; set; }
   }
}
