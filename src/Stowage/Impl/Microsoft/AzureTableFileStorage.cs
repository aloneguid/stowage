using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Microsoft
{
   /// <summary>
   /// Simulatas table storage as file storage. Effectively table storage is nothing more than
   /// a key-value storage just like blob storage, but value is not a blob but some sort of a structure.
   /// </summary>
   class AzureTableFileStorage : PolyfilledHttpFileStorage
   {
      public AzureTableFileStorage(string accountName, DelegatingHandler authHandler)
         : base(new Uri($"https://{accountName}.table.core.windows.net"), authHandler)
      {

      }

      class ListTablesResponse
      {
         public class Table
         {
            [JsonPropertyName("TableName")]
            public string Name { get; set; }
         }

         [JsonPropertyName("value")]
         public Table[] Value { get; set; }
      }



      /// <summary>
      /// Queries tables: https://docs.microsoft.com/en-us/rest/api/storageservices/query-tables
      /// </summary>
      /// <returns></returns>
      private async Task<IReadOnlyCollection<IOEntry>> ListTablesAsync()
      {
         var request = new HttpRequestMessage(HttpMethod.Get, "/Tables");
         request.Headers.Add("Accept", "application/json;odata=nometadata");
         ListTablesResponse response = await GetAsync<ListTablesResponse>(request);
         if(response == null) return new List<IOEntry>();
         return response.Value.Select(t => new IOEntry(t.Name + IOPath.PathSeparatorString)).ToList();
      }

      public override async Task<IReadOnlyCollection<IOEntry>> Ls(
         IOPath path = null, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path != null && !path.IsFolder)
            throw new ArgumentException($"{nameof(path)} needs to be a folder", nameof(path));

         if(IOPath.IsRoot(path))
            return await ListTablesAsync();

         throw new NotImplementedException();
      }

      public override Task<Stream> OpenRead(IOPath path, CancellationToken cancellationToken = default) => throw new NotImplementedException();

      /// <summary>
      /// Insert: https://docs.microsoft.com/en-us/rest/api/storageservices/insert-entity
      /// </summary>
      /// <param name="path"></param>
      /// <param name="mode"></param>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      public override async Task<Stream> OpenWrite(IOPath path, WriteMode mode, CancellationToken cancellationToken = default)
      {
         string[] parts = IOPath.Split(path);

         // 0 - table name
         // 1 - partition key
         // 2 - row key

         var request = new HttpRequestMessage(HttpMethod.Post, "/test");
         var content = new StringContent("{ \"PartitionKey\": \"p1\", \"RowKey\", \"r1\"}", null, "aplication/json");
         content.Headers.ContentType.CharSet = "";
         //content.Headers.ContentType.MediaType = "aplication/json;odata=nometadata";
         //content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "nometadata"));
         request.Content = content;
         //request.Headers.Remove("Content-Type");
         //request.Headers.TryAddWithoutValidation("Content-Type", "application/json;odata=nometadata");
         //request.Headers.Add("Accept", "application/json;odata=nometadata");
         request.Headers.Add("Accept", "application/json;odata=minimalmetadata");
         //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json;odata=nometadata");
         request.Headers.Add("Prefer", "return-no-content");

         HttpResponseMessage response = await SendAsync(request);

         return null;
      }

      public override Task Rm(IOPath path, bool recurse = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
   }
}
