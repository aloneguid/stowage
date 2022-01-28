using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class SqlUser
   {
      [JsonPropertyName("id")]
      public long Id { get; set; }

      [JsonPropertyName("name")]
      public string Name { get; set; }

      [JsonPropertyName("email")]
      public string Email { get; set; }

      [JsonPropertyName("profile_image_url")]
      public string ProfileImageUrl { get; set; }

      [JsonPropertyName("is_db_admin")]
      public bool IsDbAdmin { get; set; }
   }
}
