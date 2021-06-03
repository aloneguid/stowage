using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Amazon
{
   sealed class AwsS3FileStorage : PolyfilledHttpFileStorage
   {
      private readonly XmlResponseParser _xmlParser = new XmlResponseParser();

      public AwsS3FileStorage(string bucketName, DelegatingHandler authHandler) : 
         base(new Uri($"https://{bucketName}.s3.amazonaws.com"), authHandler)
      {
      }

      public override async Task<IReadOnlyCollection<IOEntry>> Ls(IOPath path, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path != null && !path.IsFolder)
            throw new ArgumentException("path needs to be a folder", nameof(path));

         string delimiter = recurse ? null : "/";
         string prefix = IOPath.IsRoot(path) ? null : path.NLWTS;

         // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_ListObjectsV2.html
         string uri = "/?list-type=2";
         if(delimiter != null)
            uri += "&delimiter=" + delimiter;
         if(prefix != null)
            uri += "&prefix=" + prefix;

         HttpResponseMessage response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, uri));
         response.EnsureSuccessStatusCode();
         string xml = await response.Content.ReadAsStringAsync();

         List<IOEntry> result =  _xmlParser.ParseListObjectV2Response(xml, out _).ToList();

         if(recurse)
         {
            Implicits.AssumeImplicitFolders(path, result);
         }

         return result;
      }

      public override async Task Rm(IOPath path, bool recurse, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_DeleteObject.html
         (await SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, path.NLS)))
            .EnsureSuccessStatusCode();
      }

      public override void Dispose()
      {

      }

      public override async Task<Stream> OpenRead(IOPath path, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetObject.html
         HttpResponseMessage response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/{IOPath.Normalize(path, true)}"));

         if(response.StatusCode == HttpStatusCode.NotFound)
            return null;

         response.EnsureSuccessStatusCode();

         return await response.Content.ReadAsStreamAsync();
      }

      public override async Task<Stream> OpenWrite(IOPath path, WriteMode mode, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         string npath = IOPath.Normalize(path, true);

         // initiate upload and get upload ID
         var request = new HttpRequestMessage(HttpMethod.Post, $"/{npath}?uploads");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string xml = await response.Content.ReadAsStringAsync(); // this contains UploadId
         string uploadId = _xmlParser.ParseInitiateMultipartUploadResponse(xml);

         return new AwsWriteStream(this, npath, uploadId);
      }

      // https://docs.aws.amazon.com/AmazonS3/latest/API/API_UploadPart.html
      private HttpRequestMessage CreateUploadPartRequest(string key, string uploadId, int partNumber, byte[] buffer, int count)
      {
         var request = new HttpRequestMessage(HttpMethod.Put, $"/{key}?partNumber={partNumber}&uploadId={uploadId}");
         request.Content = new ByteArrayContent(buffer, 0, count);
         return request;
      }

      public string UploadPart(string key, string uploadId, int partNumber, byte[] buffer, int count)
      {
         HttpResponseMessage response = Send(CreateUploadPartRequest(key, uploadId, partNumber, buffer, count));
         response.EnsureSuccessStatusCode();
         return response.Headers.GetValues("ETag").First();
      }

      public async Task<string> UploadPartAsync(string key, string uploadId, int partNumber, byte[] buffer, int count)
      {
         HttpResponseMessage response = await SendAsync(CreateUploadPartRequest(key, uploadId, partNumber, buffer, count));
         response.EnsureSuccessStatusCode();
         return response.Headers.GetValues("ETag").First();
      }

      //https://docs.aws.amazon.com/AmazonS3/latest/API/API_CompleteMultipartUpload.html
      private HttpRequestMessage CreateCompleteMultipartUploadRequest(string key, string uploadId, IEnumerable<string> partTags)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"/{key}?uploadId={uploadId}");

         var sb = new StringBuilder(@"<?xml version=""1.0"" encoding=""UTF-8""?><CompleteMultipartUpload xmlns=""http://s3.amazonaws.com/doc/2006-03-01/"">");
         int partId = 1;
         foreach(string eTag in partTags)
         {
            sb
               .Append("<Part><ETag>")
               .Append(eTag)
               .Append("</ETag><PartNumber>")
               .Append(partId++)
               .Append("</PartNumber></Part>");

         }
         sb.Append("</CompleteMultipartUpload>");
         request.Content = new StringContent(sb.ToString());
         return request;
      }

      public void CompleteMultipartUpload(string key, string uploadId, IEnumerable<string> partTags)
      {
         Send(CreateCompleteMultipartUploadRequest(key, uploadId, partTags)).EnsureSuccessStatusCode();
      }

      public async Task CompleteMultipartUploadAsync(string key, string uploadId, IEnumerable<string> partTags)
      {
         (await SendAsync(CreateCompleteMultipartUploadRequest(key, uploadId, partTags))).EnsureSuccessStatusCode();
      }
   }
}
