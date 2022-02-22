using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class ScimUser
   {
      [JsonPropertyName("id")]
      public string Id { get; set; }

      [JsonPropertyName("externalId")]
      public string ExternalId { get; set; }

      [JsonPropertyName("userName")]
      public string UserName { get; set; }

      [JsonPropertyName("displayName")]
      public string DisplayName { get; set; }

      [JsonPropertyName("active")]
      public bool IsActive { get; set; }

      public class Entitlement
      {
         [JsonPropertyName("value")]
         public string Value { get; set; }

         public override string ToString() => Value;
      }

      /// <summary>
      /// Seen so far:
      /// - allow-cluster-create
      /// - allow-instance-pool-create
      /// - databricks-sql-access
      /// </summary>
      [JsonPropertyName("entitlements")]
      public Entitlement[] Entitlements { get; set; }

      public class Group
      {
         [JsonPropertyName("display")]
         public string Display { get; set; }
      }

      [JsonPropertyName("groups")]
      public Group[] Groups { get; set; }

      public override string ToString() => $"{DisplayName} ({UserName})";
   }
}
