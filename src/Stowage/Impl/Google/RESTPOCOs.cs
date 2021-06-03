using System.Text.Json.Serialization;

namespace Stowage.Impl.Google
{
   class InsertObjectRequest
   {
      [JsonPropertyName("contentType")]
      public string ContentType { get; set; }

   }

   /// <summary>
   /// https://cloud.google.com/storage/docs/json_api/v1/objects#resource
   /// </summary>
   class ObjectResponse
   {
      [JsonPropertyName("id")]
      public string Id { get; set; }

      [JsonPropertyName("name")]
      public string Name { get; set; }

      [JsonPropertyName("storageClass")]
      public string StorageClass { get; set; }

      [JsonPropertyName("md5Hash")]
      public string MD5 { get; set; }

      [JsonPropertyName("crc32c")]
      public string CRC32 { get; set; }

      [JsonPropertyName("etag")]
      public string ETag { get; set; }
   }

   class ListResponse
   {
      [JsonPropertyName("kind")]
      public string Kind { get; set; }

      [JsonPropertyName("nextPageToken")]
      public string NextPageToken { get; set; }

      [JsonPropertyName("prefixes")]
      public string[] Prefixes { get; set; }

      [JsonPropertyName("items")]
      public ObjectResponse[] Items { get; set; }
   }
}
