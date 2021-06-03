using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Google
{
   /// <summary>
   /// 
   /// JSON API: https://cloud.google.com/storage/docs/json_api
   /// </summary>
   sealed class GoogleCloudStorage : PolyfilledHttpFileStorage
   {
      private readonly string _bucketName;

      public GoogleCloudStorage(string bucketName, DelegatingHandler authHandler)
         : base(new Uri($"https://storage.googleapis.com"), authHandler)
      {
         _bucketName = bucketName;
      }

      public override async Task<IReadOnlyCollection<IOEntry>> Ls(IOPath path, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path != null && !path.IsFolder)
            throw new ArgumentException("path needs to be a folder", nameof(path));

         // https://cloud.google.com/storage/docs/json_api/v1/objects/list

         string prefix = IOPath.IsRoot(path) ? null : IOPath.Normalize(path, true, true);
         string delimiter = recurse ? null : "/";
         string url = "";
         //string url = "/storage/v1/b/{bucketName}/o";
         if(null != prefix)
            url += "&prefix=" + prefix.UrlEncode();
         if(null != delimiter)
            url += "&delimiter=" + delimiter.UrlEncode();
         if(url.Length > 1)
            url = "/?" + url.Substring(1);
         url = $"/storage/v1/b/{_bucketName}/o" + url;
         var request = new HttpRequestMessage(HttpMethod.Get, url);
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         ListResponse lr = JsonSerializer.Deserialize<ListResponse>(rjson);

         var result =  ConvertBatch(lr).ToList();

         if(recurse)
         {
            Implicits.AssumeImplicitFolders(path, result);
         }

         return result;
      }

      private static IEnumerable<IOEntry> ConvertBatch(ListResponse lr)
      {
         return Enumerable.Empty<IOEntry>()
            .Concat(lr.Items == null ? Enumerable.Empty<IOEntry>() : lr.Items.Select(i => new IOEntry(i.Name)))
            .Concat(lr.Prefixes == null ? Enumerable.Empty<IOEntry>() : lr.Prefixes.Select(i => new IOEntry(i)));

      }

      // https://cloud.google.com/storage/docs/performing-resumable-uploads#initiate-session
      private HttpRequestMessage CreateInitiateResumableUploadRequest(string objectName)
      {
         var request = new HttpRequestMessage(HttpMethod.Post,
            $"/upload/storage/v1/b/{_bucketName}/o?uploadType=resumable&name={objectName}");
         return request;
      }

      public string InitiateResumableUpload(string objectName)
      {
         HttpResponseMessage response = Send(CreateInitiateResumableUploadRequest(objectName));
         response.EnsureSuccessStatusCode();
         return response.Headers.Location.ToString();
      }

      public async Task<string> InitiateResumableUploadAsync(string objectName)
      {
         HttpResponseMessage response = await SendAsync(CreateInitiateResumableUploadRequest(objectName));
         response.EnsureSuccessStatusCode();
         return response.Headers.Location.ToString();
      }

      private HttpRequestMessage CreateResumeUploadRequest(string sessionUri, byte[] buffer, int count)
      {
         //probably need content-range: https://cloud.google.com/storage/docs/performing-resumable-uploads#initiate-session
         var request = new HttpRequestMessage(HttpMethod.Put, sessionUri);
         request.Content = new ByteArrayContent(buffer, 0, count);
         return request;
      }

      public void ResumeUpload(string sessionUri, byte[] buffer, int count)
      {
         HttpResponseMessage response = Send(CreateResumeUploadRequest(sessionUri, buffer, count));
         response.EnsureSuccessStatusCode();
      }

      public async Task ResumeUploadAsync(string sessionUri, byte[] buffer, int count)
      {
         HttpResponseMessage response = await SendAsync(CreateResumeUploadRequest(sessionUri, buffer, count));
         response.EnsureSuccessStatusCode();
      }

      public override Task<Stream> OpenWrite(IOPath path, WriteMode mode, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         return Task.FromResult<Stream>(new GoogleWriteStream(this, IOPath.Normalize(path, true)));
      }

      public override async Task<Stream> OpenRead(IOPath path, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));


         // call https://cloud.google.com/storage/docs/json_api/v1/objects/get
         // alt - Type of data to return. Defaults to json. json: Return object metadata. media: Return object data.
         HttpResponseMessage response = await SendAsync(
            new HttpRequestMessage(
               HttpMethod.Get,
               $"/storage/v1/b/{_bucketName}/o/{IOPath.Normalize(path, true).UrlEncode()}?alt=media"));

         if(response.StatusCode == HttpStatusCode.NotFound)
            return null;

         response.EnsureSuccessStatusCode();

         return await response.Content.ReadAsStreamAsync();
      }

      public override async Task Rm(IOPath path, bool recurse, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         if(recurse)
         {
            await RmRecurseWithLs(path, cancellationToken);
         }
         else
         {
            // See https://cloud.google.com/storage/docs/json_api/v1/objects/delete
            HttpResponseMessage response = await SendAsync(new HttpRequestMessage(HttpMethod.Delete,
               $"/storage/v1/b/{_bucketName}/o/{path.NLS.UrlEncode()}"));

            if(response.StatusCode != HttpStatusCode.NotFound)
               response.EnsureSuccessStatusCode();
         }
      }
   }
}
