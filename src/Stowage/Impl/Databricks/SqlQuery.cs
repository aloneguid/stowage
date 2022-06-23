using System;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// SQL Analytics Query Description
   /// </summary>
   public class SqlQuery
   {
      /// <summary>
      /// Unique Id
      /// </summary>
      /// <example>dee5cca8-1c79-4b5e-a711-e7f9d241bdf6</example>
      [JsonPropertyName("id")]
      public string Id { get; set; }

      /// <summary>
      /// The title of this query that appears in list views, widget headings, and on the query page.
      /// </summary>
      [JsonPropertyName("name")]
      public string Name { get; set; }

      /// <summary>
      /// Array of tags
      /// </summary>
      [JsonPropertyName("tags")]
      public string[] Tags { get; set; }

      /// <summary>
      /// Name
      /// </summary>
      public override string ToString() => Name;

      /// <summary>
      /// General description that conveys additional information about this query such as usage notes.
      /// </summary>
      [JsonPropertyName("description")]
      public string Description { get; set; }

      /// <summary>
      /// Describes whether the authenticated user may edit the definition of this query.
      /// </summary>
      [JsonPropertyName("can_edit")]
      public bool? CanEdit { get; set; }

      /// <summary>
      /// The text of the query to be run.
      /// </summary>
      /// <example>
      /// SELECT field FROM table WHERE field = {{ param }}
      /// </example>
      [JsonPropertyName("query")]
      public string SqlExpression { get; set; }

      /// <summary>
      /// Whether the query is trashed. Trashed queries can't be used in dashboards, or appear in search results. If this boolean is `true`, the `options` property for this query will include a `moved_to_trash_at` timestamp. Trashed queries are permanently deleted after 30 days.
      /// </summary>
      [JsonPropertyName("is_archived")]
      public bool IsArchived { get; set; }

      /// <summary>
      /// Whether the query is a draft. Draft queries only appear in list views for their owners. Visualizations from draft queries cannot appear on dashboards.
      /// </summary>
      [JsonPropertyName("is_draft")]
      public bool IsDraft { get; set; }

      /// <summary>
      /// Whether this query object appears in the current user's favorites list. This flag determines whether the star icon for favorites is colored in.
      /// </summary>
      [JsonPropertyName("is_favorite")]
      public bool? IsFavorite { get; set; }

      /// <summary>
      /// Text parameter types are not safe from SQL injection for all types of data source. Set this Boolean parameter to `true` if a query either does not use any text type parameters or uses a data source type where text type parameters are handled safely.
      /// </summary>
      [JsonPropertyName("is_safe")]
      public bool? IsSafe { get; set; }

      /// <summary>
      /// The timestamp when this query was created.
      /// </summary>
      [JsonPropertyName("created_at")]
      public DateTimeOffset CreatedTime { get; set; }

      /// <summary>
      /// The timestamp at which this query was last updated.
      /// </summary>
      [JsonPropertyName("updated_at")]
      public DateTimeOffset UpdatedTime { get; set; }

      /// <summary>
      /// Data Source ID. The UUID that uniquely identifies this data source / SQL Endpoint across the API.
      /// </summary>
      /// <example>
      /// 0c205e24-5db2-4940-adb1-fb13c7ce960b
      /// </example>
      [JsonPropertyName("data_source_id")]
      public string DataSourceId { get; set; }

      /// <summary>
      /// Schedule
      /// </summary>
      [JsonPropertyName("schedule")]
      public QueryInterval Schedule { get; set; }
   }
}
