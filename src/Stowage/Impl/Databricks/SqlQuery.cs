using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class SqlQueryBase
   {
      [JsonPropertyName("id")]
      public string Id { get; set; }

      [JsonPropertyName("name")]
      public string Name { get; set; }
   }

   public class SqlQuery : SqlQueryBase
   {
      [JsonPropertyName("description")]
      public string Description { get; set; }

      [JsonPropertyName("query")]
      public string SqlExpression { get; set; }

      public override string ToString() => Name;
   }
}
