using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{

   class ErrorResponse
   {
      [JsonPropertyName("error_code")]
      public string Code { get; set; }

      [JsonPropertyName("message")]
      public string Message { get; set; }
   }

   class FileInfo
   {
      [JsonPropertyName("path")]
      public string Path { get; set; }

      [JsonPropertyName("is_dir")]
      public bool IsDir { get; set; }

      /// <summary>
      /// The length of the file in bytes or zero if the path is a directory.
      /// </summary>
      [JsonPropertyName("file_size")]
      public long FileSize { get; set; }

      /// <summary>
      /// The last time, in epoch milliseconds, the file or directory was modified.
      /// </summary>
      [JsonPropertyName("modification_time")]
      public long ModTime { get; set; }
   }

   class ListRequest
   {
      [JsonPropertyName("path")]
      public string Path { get; set; }
   }

   class ListResponse
   {
      [JsonPropertyName("files")]
      public FileInfo[] Files { get; set; }
   }

   class CreateRequest
   {
      [JsonPropertyName("path")]
      public string Path { get; set; }

      [JsonPropertyName("overwrite")]
      public bool Overwrite { get; set; }
   }

   class CreateResponse
   {
      [JsonPropertyName("handle")]
      public long Handle { get; set; }
   }

   class AddBlockRequest
   {
      [JsonPropertyName("data")]
      public string Base64Data { get; set; }

      [JsonPropertyName("handle")]
      public long Handle { get; set; }
   }

   class CloseRequest
   {
      [JsonPropertyName("handle")]
      public long Handle { get; set; }
   }

   class GetStatusRequest
   {
      [JsonPropertyName("path")]
      public string Path { get; set; }
   }

   class ReadRequest
   {
      [JsonPropertyName("path")]
      public string Path { get; set; }

      [JsonPropertyName("offset")]
      public long Offset { get; set; }

      [JsonPropertyName("length")]
      public long Count { get; set; }
   }

   class ReadResponse
   {
      [JsonPropertyName("bytes_read")]
      public long Read { get; set; }

      [JsonPropertyName("data")]
      public string Base64EncodedData { get; set; }
   }

   class DeleteRequest
   {
      [JsonPropertyName("path")]
      public string Path { get; set; }

      [JsonPropertyName("recursive")]
      public bool Recursive { get; set; } = true;
   }
}
