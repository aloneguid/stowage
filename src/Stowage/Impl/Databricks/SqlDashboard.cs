using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// SQL Analytics Dashboard
   /// </summary>
   public class SqlDashboard
   {
      /// <summary>
      /// The ID for this dashboard.
      /// </summary>
      [JsonPropertyName("id")]
      public string Id { get; set; }

      /// <summary>
      /// The title of the dashboard that appears in list views and at the top of the dashboard page.
      /// </summary>
      [JsonPropertyName("name")]
      public string Name { get; set; }

      /// <summary>
      /// URL slug. Usually mirrors the query name with dashes (`-`) instead of spaces. Appears in the URL for this query.
      /// </summary>
      [JsonPropertyName("slug")]
      public string Slug { get; set; }

      /// <summary>
      /// Whether the authenticated user can edit the query definition.
      /// </summary>
      [JsonPropertyName("can_edit")]
      public bool CanEdit { get; set; }

      /// <summary>
      /// In the web application, query filters that share a name are coupled to a single selection box if this value is `true`.
      /// </summary>
      [JsonPropertyName("dashboard_filters_enabled")]
      public bool DashboardsFiltersEnabled { get; set; }

      /// <summary>
      /// Whether a dashboard is trashed. Trashed dashboards won't appear in list views.  If this boolean is `true`, the `options` property for this dashboard will include a `moved_to_trash_at` timestamp. Items in Trash are permanently deleted after 30 days.
      /// </summary>
      [JsonPropertyName("is_archived")]
      public bool IsArchived { get; set; }

      /// <summary>
      /// Whether a dashboard is a draft. Draft dashboards only appear in list views for their owners.
      /// </summary>
      [JsonPropertyName("is_draft")]
      public bool IsDraft { get; set; }

      /// <summary>
      /// Associated tag
      /// </summary>
      [JsonPropertyName("tags")]
      public string[] Tags { get; set; }

      /// <summary>
      /// Timestamp when this dashboard was created.
      /// </summary>
      [JsonPropertyName("created_at")]
      public DateTimeOffset CreatedTime { get; set; }

      /// <summary>
      /// Timestamp when this dashboard was last updated.
      /// </summary>
      [JsonPropertyName("updated_at")]
      public DateTimeOffset UpdatedTime { get; set; }

      /// <summary>
      /// Whether this query object appears in the current user's favorites list. This flag determines whether the star icon for favorites is colored in.
      /// </summary>
      [JsonPropertyName("is_favorite")]
      public bool IsFavourite { get; set; }

      /// <summary>
      /// The user that created and owns this dashboard.
      /// </summary>
      [JsonPropertyName("user")]
      public SqlUser User { get; set; }

      /// <summary>
      /// enum: CAN_VIEW, CAN_RUN, CAN_MANAGE
      /// </summary>
      [JsonPropertyName("permission_tier")]
      public string PermissionTier { get; set; }

      /// <summary>
      /// Extra options like "run as" role, param order.
      /// </summary>
      [JsonPropertyName("options")]
      public Dictionary<string, object> Options { get; set; }

      /// <summary>
      /// Visual widgets.
      /// </summary>
      [JsonPropertyName("widgets")]
      public Widget[] Widgets { get; set; }

      /// <summary>
      /// Returns name;
      /// </summary>
      /// <returns></returns>
      public override string ToString() => Name;
   }
}
