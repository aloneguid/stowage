using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/workspace#--objectinfo
   /// </summary>
   public class ObjectInfo
   {
      [JsonPropertyName("object_id")]
      public long Id { get; set; }

      [JsonPropertyName("path")]
      public string Path { get; set; }

      /// <summary>
      /// One of: NOTEBOOK, DIRECTORY, LIBRARY, REPO
      /// </summary>
      [JsonPropertyName("object_type")]
      public string Type { get; set; }

      /// <summary>
      /// SCALA, PYTHON, SQL or R.
      /// </summary>
      [JsonPropertyName("language")]
      public string Language { get; set; }

      public override string ToString() => $"{Path} ({Type}, {Language})";
   }
}
