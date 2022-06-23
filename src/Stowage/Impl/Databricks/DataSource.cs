using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class DataSource
   {
      [JsonPropertyName("id")]
      public string Id { get; set; }

      [JsonPropertyName("endpoint_id")]
      public string EndpointId { get; set; }

      [JsonPropertyName("name")]
      public string Name { get; set; }

      [JsonPropertyName("pause_reason")]
      public string PauseReason { get; set; }

      [JsonPropertyName("paused")]
      public int Paused { get; set; }

      [JsonPropertyName("supports_auto_limit")]
      public bool SupportsAutoLimit { get; set; }

      [JsonPropertyName("syntax")]
      public string Syntax { get; set; }

      [JsonPropertyName("type")]
      public string Type { get; set; }

      [JsonPropertyName("view_only")]
      public bool IsViewOnly { get; set; }

      /// <summary>
      /// Name
      /// </summary>
      /// <returns></returns>
      public override string ToString() => Name;
   }
}
