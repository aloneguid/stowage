using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Stowage.Impl.Microsoft
{
   sealed class AzureBlobFileStorage : PolyfilledHttpFileStorage
   {
      // https://docs.microsoft.com/en-us/rest/api/storageservices/blob-service-rest-api

      // auth sample - https://github.com/Azure-Samples/storage-dotnet-rest-api-with-auth/tree/master/StorageRestApiAuth

      // auth with SharedKey - https://docs.microsoft.com/en-us/rest/api/storageservices/authorize-with-shared-key

      private readonly string _containerName;

      public AzureBlobFileStorage(string accountName, string containerName, DelegatingHandler authHandler)
         : base(new Uri($"https://{accountName}.blob.core.windows.net"), authHandler)
      {
         if(string.IsNullOrEmpty(accountName))
            throw new ArgumentException($"'{nameof(accountName)}' cannot be null or empty", nameof(accountName));
         if(string.IsNullOrEmpty(containerName))
            throw new ArgumentException($"'{nameof(containerName)}' cannot be null or empty", nameof(containerName));
         if(authHandler is null)
            throw new ArgumentNullException(nameof(authHandler));

         _containerName = containerName;
      }

      public override Task<IReadOnlyCollection<IOEntry>> Ls(IOPath path, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path != null && !path.IsFolder)
            throw new ArgumentException($"{nameof(path)} needs to be a folder", nameof(path));

         return ListAsync(path, recurse, cancellationToken);
      }

      private static IEnumerable<IOEntry> ConvertBatch(XElement blobs)
      {
         foreach(XElement blobPrefix in blobs.Elements("BlobPrefix"))
         {
            string name = blobPrefix.Element("Name").Value;
            yield return new IOEntry(name + IOPath.PathSeparatorString);
         }

         foreach(XElement blob in blobs.Elements("Blob"))
         {
            string name = blob.Element("Name").Value;
            var file = new IOEntry(name);

            foreach(XElement xp in blob.Element("Properties").Elements())
            {
               string pname = xp.Name.ToString();
               string pvalue = xp.Value;

               if(!string.IsNullOrEmpty(pvalue))
               {
                  if(pname == "Last-Modified")
                  {
                     file.LastModificationTime = DateTimeOffset.Parse(pvalue);
                  }
                  else if(pname == "Content-Length")
                  {
                     file.Size = long.Parse(pvalue);
                  }
                  else if(pname == "Content-MD5")
                  {
                     file.MD5 = pvalue;
                  }
                  else
                  {
                     file.Properties[pname] = pvalue;
                  }
               }
            }

            yield return file;
         }
      }

      // see https://docs.microsoft.com/en-us/rest/api/storageservices/list-blobs
      private async Task<string> ListAsync(string containerName,
         string prefix = null,
         string delimiter = null,
         string include = "metadata")
      {
         string url = $"/{containerName}?restype=container&comp=list&include={include}";

         if(prefix != null)
            url += "&prefix=" + prefix;

         if(delimiter != null)
            url += "&delimiter=" + delimiter;

         HttpResponseMessage response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
         response.EnsureSuccessStatusCode();
         return await response.Content.ReadAsStringAsync();
      }

      private async Task<IReadOnlyCollection<IOEntry>> ListAsync(
         string path, bool recurse,
         CancellationToken cancellationToken)
      {
         var result = new List<IOEntry>();

         // maxResults default is 5'000

         string prefix = GetPathInContainer(path);

         string rawXml = await ListAsync(GetContainerName(path),
            IOPath.IsRoot(prefix) ? null : (prefix.Trim('/') + "/"),
            delimiter: recurse ? null : "/");

         XElement x = XElement.Parse(rawXml);
         XElement blobs = x.Element("Blobs");
         if(blobs != null)
         {
            result.AddRange(ConvertBatch(blobs));
         }

         XElement nextMarker = x.Element("NextMarker");

         if(recurse)
         {
            Implicits.AssumeImplicitFolders(path, result);
         }

         return result;
      }

      public override Task<Stream> OpenWrite(IOPath path, WriteMode mode, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         return Task.FromResult<Stream>(new AzureWriteStream(this, GetContainerName(path), GetPathInContainer(path)));
      }

      private HttpRequestMessage CreatePutBlockRequest(int blockId, string containerName, string blobName, byte[] buffer, int count, out string blockIdStr)
      {
         blockIdStr = Convert.ToBase64String(Encoding.ASCII.GetBytes(blockId.ToString()));

         // call https://docs.microsoft.com/en-us/rest/api/storageservices/put-block
         var request = new HttpRequestMessage(HttpMethod.Put, $"/{containerName}/{blobName}?comp=block&blockid={blockIdStr}");
         request.Content = new ByteArrayContent(buffer, 0, count);
         return request;
      }

      public string PutBlock(int blockId, string containerName, string blobName, byte[] buffer, int count)
      {
         HttpRequestMessage request = CreatePutBlockRequest(blockId, containerName, blobName, buffer, count, out string blockIdStr);
         HttpResponseMessage response = Send(request);
         response.EnsureSuccessStatusCode();
         return blockIdStr;
      }

      public async Task<string> PutBlockAsync(int blockId, string containerName, string blobName, byte[] buffer, int count)
      {
         HttpRequestMessage request = CreatePutBlockRequest(blockId, containerName, blobName, buffer, count, out string blockIdStr);
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         return blockIdStr;
      }

      private HttpRequestMessage CreatePutBlockListRequest(string containerName, string blobName, IEnumerable<string> blockIds)
      {
         /* sample body
            Request Body:  
            <?xml version="1.0" encoding="utf-8"?>  
            <BlockList>  
            <Latest>AAAAAA==</Latest>  
            <Latest>AQAAAA==</Latest>  
            <Latest>AZAAAA==</Latest>  
            </BlockList> */

         var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?><BlockList>");
         foreach(string id in blockIds)
         {
            sb.Append("<Latest>").Append(id).Append("</Latest>");
         }
         sb.Append("</BlockList>");

         // call https://docs.microsoft.com/en-us/rest/api/storageservices/put-block-list
         var request = new HttpRequestMessage(HttpMethod.Put, $"/{containerName}/{blobName}?comp=blocklist");
         request.Content = new StringContent(sb.ToString());
         return request;
      }

      public void PutBlockList(string containerName, string blobName, IEnumerable<string> blockIds)
      {
         HttpResponseMessage response = Send(CreatePutBlockListRequest(containerName, blobName, blockIds));
         response.EnsureSuccessStatusCode();
      }

      public async Task PutBlockListAsync(string containerName, string blobName, IEnumerable<string> blockIds)
      {
         HttpResponseMessage response = await SendAsync(CreatePutBlockListRequest(containerName, blobName, blockIds));
         response.EnsureSuccessStatusCode();
      }

      // https://docs.microsoft.com/en-us/rest/api/storageservices/put-blob
      /*public async Task PutBlobAsync()
      {
         
      }*/

      //todo: remove both, as we're scoped to containers
      private string GetContainerName(string path)
      {
         return _containerName ?? path.Split(IOPath.PathSeparatorChar, 2)[0];
      }

      private string GetPathInContainer(string path)
      {
         return IOPath.Normalize(_containerName == null ? IOPath.RemoveRoot(path) : path, true);
      }

      public override async Task<Stream> OpenRead(IOPath path, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         // call https://docs.microsoft.com/en-us/rest/api/storageservices/get-blob
         var request = new HttpRequestMessage(HttpMethod.Get, $"/{GetContainerName(path)}/{GetPathInContainer(path)}");
         HttpResponseMessage response = await SendAsync(request);

         if(response.StatusCode == HttpStatusCode.NotFound)
            return null;

         response.EnsureSuccessStatusCode();

         return await response.Content.ReadAsStreamAsync();


         //ApiResponse<Stream> response = await _api.GetBlob(GetContainerName(path), GetPathInContainer(path));

         //return response.Content;
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
            https://docs.microsoft.com/en-us/rest/api/storageservices/delete-blob
            HttpResponseMessage response = await SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/{_containerName}{path}"));
            if(response.StatusCode != HttpStatusCode.NotFound)
            {
               response.EnsureSuccessStatusCode();
            }
         }
      }
   }
}
