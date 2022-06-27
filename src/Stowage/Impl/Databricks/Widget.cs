using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Stowage.Impl.Databricks
{
   public class Widget
   {
      /// <summary>
      /// The unique ID for this widget.
      /// </summary>
      [JsonPropertyName("id")]
      public string Id { get; set; }

      [JsonPropertyName("options")]
      public Dictionary<string, object> Options { get; set; }

      [JsonPropertyName("visualization")]
      public Visualisation Visualisation { get; set; }
   }
}
