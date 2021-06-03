using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// https://docs.databricks.com/dev-tools/api/latest/dbfs.html
   /// </summary>
   sealed class DatabricksDbfsStorage : PolyfilledHttpFileStorage
   {
      private readonly string _base;

      public DatabricksDbfsStorage(Uri instanceUri, string token) : base(instanceUri, new StaticAuthHandler(token))
      {
         _base = instanceUri.ToString().TrimEnd('/') + "/api/2.0/dbfs";
      }

      /// <summary>
      /// https://docs.databricks.com/dev-tools/api/latest/dbfs.html#list
      /// </summary>
      /// <param name="path"></param>
      /// <param name="recurse"></param>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      public override async Task<IReadOnlyCollection<IOEntry>> Ls(IOPath path = null, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path != null && !path.IsFolder)
            throw new ArgumentException("path needs to be a folder", nameof(path));

         var result = new List<IOEntry>();
         await Ls(path, result, recurse);
         return result;
      }

      private async Task Ls(IOPath path, List<IOEntry> container, bool recurse)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_base}/list");
         request.Content = new StringContent(JsonSerializer.Serialize(new ListRequest { Path = path ?? IOPath.Root }));
         HttpResponseMessage response = await SendAsync(request);
         if(response.StatusCode == HttpStatusCode.NotFound)
            return;
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         ListResponse lr = JsonSerializer.Deserialize<ListResponse>(rjson);

         if((lr?.Files?.Length ?? 0) == 0)
            return;

         var batch = lr.Files.Select(fi => new IOEntry(fi.IsDir ? fi.Path + "/" : fi.Path) { Size = fi.IsDir ? null : fi.FileSize }).ToList();
         container.AddRange(batch);

         if(recurse)
         {
            foreach(IOEntry folder in batch.Where(e => e.Path.IsFolder))
            {
               await Ls(folder.Path, container, recurse);
            }
         }
      }

      public override async Task<Stream> OpenRead(IOPath path, CancellationToken cancellationToken = default)
      {
         // get status to determine length
         FileInfo fi = await Dbfs<GetStatusRequest, FileInfo>
            ("get-status",
            new GetStatusRequest { Path = path ?? IOPath.Root },
            HttpMethod.Get);
         if(fi == null)
            return null;

         //
         return new ReadStream(this, path, fi.FileSize);
      }

      private HttpRequestMessage CreateReadRequest(IOPath path, long offset, long count)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_base}/read");
         request.Content = new StringContent(JsonSerializer.Serialize(new ReadRequest { Path = path, Offset = offset, Count = count }));
         return request;
      }

      // https://docs.databricks.com/dev-tools/api/latest/dbfs.html#read
      public byte[] Read(IOPath path, long offset, long count)
      {
         HttpResponseMessage response = Send(CreateReadRequest(path, offset, count));
         response.EnsureSuccessStatusCode();
         ReadResponse rr = JsonSerializer.Deserialize<ReadResponse>(response.Content.ReadAsStringAsync().Result);
         return Convert.FromBase64String(rr.Base64EncodedData);
      }

      public async Task<byte[]> ReadAsync(IOPath path, long offset, long count)
      {
         HttpResponseMessage response = await SendAsync(CreateReadRequest(path, offset, count));
         response.EnsureSuccessStatusCode();
         ReadResponse rr = JsonSerializer.Deserialize<ReadResponse>(await response.Content.ReadAsStringAsync());
         return Convert.FromBase64String(rr.Base64EncodedData);
      }

      public override async Task<Stream> OpenWrite(IOPath path, WriteMode mode, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         //create upload first: https://docs.databricks.com/dev-tools/api/latest/dbfs.html#create
         CreateResponse cr = await Dbfs<CreateRequest, CreateResponse>("create", new CreateRequest { Path = path, Overwrite = true });

         return new WriteStream(this, cr.Handle);
      }

      private HttpRequestMessage CreateAddBlockRequest(long handle, byte[] buffer, int count)
      {
         // https://docs.databricks.com/dev-tools/api/latest/dbfs.html#add-block
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_base}/add-block");
         request.Content = new StringContent(JsonSerializer.Serialize(
            new AddBlockRequest { Handle = handle, Base64Data = Convert.ToBase64String(buffer, 0, count) }));
         return request;
      }

      public void AddBlock(long handle, byte[] buffer, int count)
      {
         Send(CreateAddBlockRequest(handle, buffer, count)).EnsureSuccessStatusCode();
      }

      public async Task AddBlockAsync(long handle, byte[] buffer, int count)
      {
         (await SendAsync(CreateAddBlockRequest(handle, buffer, count))).EnsureSuccessStatusCode();
      }

      private HttpRequestMessage CreateCloseRequest(long handle)
      {
         // https://docs.databricks.com/dev-tools/api/latest/dbfs.html#close
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_base}/close");
         request.Content = new StringContent(JsonSerializer.Serialize(new CloseRequest { Handle = handle }));
         return request;
      }

      public void Close(long handle)
      {
         Send(CreateCloseRequest(handle)).EnsureSuccessStatusCode();
      }

      public async Task CloseAsync(long handle)
      {
         (await SendAsync(CreateCloseRequest(handle))).EnsureSuccessStatusCode();
      }


      public override async Task Rm(IOPath path, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         // https://docs.databricks.com/dev-tools/api/latest/dbfs.html#delete

         await Dbfs<DeleteRequest, object>("delete", new DeleteRequest { Path = path, Recursive = recurse });
      }

      private async Task<TResponse> Dbfs<TRequest, TResponse>(string command, TRequest request, HttpMethod method = null)
      {
         var httpRequest = new HttpRequestMessage(method ?? HttpMethod.Post, $"{_base}/{command}");
         httpRequest.Content = new StringContent(JsonSerializer.Serialize(request));
         HttpResponseMessage httpResponse = await SendAsync(httpRequest);

         if(httpResponse.StatusCode == HttpStatusCode.BadRequest)
         {
            ErrorResponse error = JsonSerializer.Deserialize<ErrorResponse>(await httpResponse.Content.ReadAsStringAsync());
            throw new IOException($"DBFS '{command}' operation failed with code {error?.Code}. Message: {error?.Message}");
         }
         else if(httpResponse.StatusCode == HttpStatusCode.NotFound)
         {
            return default(TResponse);
         }

         httpResponse.EnsureSuccessStatusCode();

         if(typeof(TResponse) == typeof(object))
            return default(TResponse);

         return JsonSerializer.Deserialize<TResponse>(await httpResponse.Content.ReadAsStringAsync());
      }
   }
}
