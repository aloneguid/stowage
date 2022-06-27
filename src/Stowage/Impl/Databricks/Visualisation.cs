using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class Visualisation
   {
      [JsonPropertyName("description")]
      public string Description { get; set; }

      [JsonPropertyName("properties")]
      public Dictionary<string, object> Properties { get; set; }
   }
}
