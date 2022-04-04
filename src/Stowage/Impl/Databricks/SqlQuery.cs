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

      public override string ToString() => Name;
   }

   public class SqlQuery : SqlQueryBase
   {
      [JsonPropertyName("description")]
      public string Description { get; set; }

      [JsonPropertyName("query")]
      public string SqlExpression { get; set; }

      [JsonPropertyName("is_archived")]
      public bool IsArchived { get; set; }

      [JsonPropertyName("is_draft")]
      public bool IsDraft { get; set; }

      [JsonPropertyName("created_at")]
      public DateTimeOffset CreatedTime { get; set; }

      [JsonPropertyName("updated_at")]
      public DateTimeOffset UpdatedTime { get; set; }

      /// <summary>
      /// This is not the same as endpoint ID.
      /// </summary>
      [JsonPropertyName("data_source_id")]
      public string DataSourceId { get; set; }

   }
}
