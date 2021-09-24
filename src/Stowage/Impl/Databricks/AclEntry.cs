using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class AclEntry
   {
      [JsonPropertyName("user_name")]
      public string UserName { get; set; }

      [JsonPropertyName("group_name")]
      public string GroupName { get; set; }

      /// <summary>
      /// One of: CAN_VIEW, CAN_RUN, CAN_MANAGE
      /// </summary>
      [JsonPropertyName("permission_level")]
      public string PermissionLevel { get; set; }

      public override string ToString() => $"{UserName ?? GroupName}: {PermissionLevel}";
   }
}
