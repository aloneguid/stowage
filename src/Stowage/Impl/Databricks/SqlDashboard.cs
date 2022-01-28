using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class SqlDashboardBase
   {
      [JsonPropertyName("id")]
      public string Id { get; set; }

      [JsonPropertyName("name")]
      public string Name { get; set; }

      [JsonPropertyName("slug")]
      public string Slug { get; set; }

      [JsonPropertyName("is_archived")]
      public bool IsArchived { get; set; }

      [JsonPropertyName("is_draft")]
      public bool IsDraft { get; set; }

      [JsonPropertyName("tags")]
      public string[] Tags { get; set; }

      [JsonPropertyName("created_at")]
      public DateTimeOffset CreatedTime { get; set; }

      [JsonPropertyName("updated_at")]
      public DateTimeOffset UpdatedTime { get; set; }

      [JsonPropertyName("version")]
      public int Version { get; set; }

      [JsonPropertyName("is_favorite")]
      public bool IsFavourite { get; set; }

      [JsonPropertyName("user")]
      public SqlUser User { get; set; }

      public override string ToString() => Name;
   }

   public class SqlDashboard : SqlDashboardBase
   {
      [JsonPropertyName("description")]
      public string Description { get; set; }

      [JsonPropertyName("query")]
      public string SqlExpression { get; set; }
   }
}
