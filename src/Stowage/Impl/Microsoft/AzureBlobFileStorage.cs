using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Stowage.Impl.Microsoft {
    sealed class AzureBlobFileStorage : PolyfilledHttpFileStorage, IAzureBlobFileStorage {
        // https://docs.microsoft.com/en-us/rest/api/storageservices/blob-service-rest-api

        // auth sample - https://github.com/Azure-Samples/storage-dotnet-rest-api-with-auth/tree/master/StorageRestApiAuth

        // auth with SharedKey - https://docs.microsoft.com/en-us/rest/api/storageservices/authorize-with-shared-key

        public AzureBlobFileStorage(string accountName, DelegatingHandler authHandler)
           : base(new Uri($"https://{accountName}.blob.core.windows.net/"), authHandler) {
            if(string.IsNullOrEmpty(accountName))
                throw new ArgumentException($"'{nameof(accountName)}' cannot be null or empty", nameof(accountName));
            if(authHandler is null)
                throw new ArgumentNullException(nameof(authHandler));
        }

        public AzureBlobFileStorage(Uri endpoint, DelegatingHandler authHandler)
           : base(EnsureUriEndsWithSlash(endpoint), authHandler) {
            if(authHandler is null)
                throw new ArgumentNullException(nameof(authHandler));
        }

        private static Uri EnsureUriEndsWithSlash(Uri endpoint)
           => endpoint.OriginalString.EndsWith(IOPath.PathSeparator)
              ? endpoint
              : new Uri($"{endpoint.OriginalString}{IOPath.PathSeparator}");

        public override async Task<IReadOnlyCollection<IOEntry>> Ls(IOPath? path, bool recurse = false, CancellationToken cancellationToken = default) {
            if(path != null && !path.IsFolder)
                throw new ArgumentException($"{nameof(path)} needs to be a folder", nameof(path));

            IReadOnlyCollection<IOEntry> result;
            if(path == null || path.IsRootPath) {
                result = await ListContainersAsync();
            } else {
                result = await ListBlobsAsync(path, recurse, cancellationToken);
                path.ExtractPrefixAndRelativePath(out string containerName, out _);
                PrependContainerName(result, containerName);
            }
            return result;
        }

        private static IEnumerable<IOEntry> ConvertBlobBatch(XElement blobs) {
            // https://learn.microsoft.com/en-us/rest/api/storageservices/list-blobs

            foreach(XElement blobPrefix in blobs.Elements("BlobPrefix")) {
                string? name = blobPrefix.Element("Name")?.Value;
                yield return new IOEntry(name + IOPath.PathSeparatorString);
            }

            foreach(XElement blob in blobs.Elements("Blob")) {
                string? name = blob.Element("Name")?.Value;
                if(name == null)
                    continue;
                var file = new IOEntry(name);

                foreach(XElement xp in blob.Element("Properties")?.Elements()) {
                    string pname = xp.Name.ToString();
                    string pvalue = xp.Value;

                    if(!string.IsNullOrEmpty(pvalue)) {
                        if(pname == "Last-Modified") {
                            file.LastModificationTime = DateTimeOffset.Parse(pvalue);
                        } else if(pname == "Content-Length") {
                            file.Size = long.Parse(pvalue);
                        } else if(pname == "Content-MD5") {
                            file.MD5 = pvalue;
                        } else {
                            file.Properties[pname] = pvalue;
                        }
                    }
                }

                yield return file;
            }
        }

        private static IEnumerable<IOEntry> ConvertContainerBatch(XElement root) {

            // https://learn.microsoft.com/en-us/rest/api/storageservices/list-containers2?tabs=microsoft-entra-id#response-body

            XElement? containers = root.Element("Containers");
            if(containers == null)
                yield break;

            foreach(XElement container in containers.Elements("Container")) {
                string? name = container.Element("Name")?.Value;
                if(name == null)
                    continue;
                var file = new IOEntry(name);

                IEnumerable<XElement>? properties = container.Element("Properties")?.Elements();
                if(properties == null)
                    continue;

                foreach(XElement xp in properties) {
                    string pname = xp.Name.ToString();
                    string pvalue = xp.Value;

                    if(!string.IsNullOrEmpty(pvalue)) {
                        if(pname == "Last-Modified") {
                            file.LastModificationTime = DateTimeOffset.Parse(pvalue);
                        } else {
                            file.Properties[pname] = pvalue;
                        }
                    }
                }

                yield return file;
            }
        }

        // see https://docs.microsoft.com/en-us/rest/api/storageservices/list-blobs
        private async Task<string> ListBlobsAsync(IOPath path,
            string? prefix = null,
            string? delimiter = null,
            string include = "metadata") {
            string url = $"{path.NLS}?restype=container&comp=list&include={include}";

            if(prefix != null)
                url += "&prefix=" + prefix;

            if(delimiter != null)
                url += "&delimiter=" + delimiter;

            HttpResponseMessage response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<IReadOnlyCollection<IOEntry>> ListContainersAsync() {
            // write call according to https://learn.microsoft.com/en-us/rest/api/storageservices/list-containers2
            string url = "?comp=list&include=metadata,deleted,system";
            HttpResponseMessage response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
            string rawXml = await response.Content.ReadAsStringAsync();

            if(!response.IsSuccessStatusCode) {
                if(AzureException.TryCreateFromXml(rawXml, null, out AzureException? ex))
                    throw ex!;
                response.EnsureSuccessStatusCode();
            }

            return ConvertContainerBatch(XElement.Parse(rawXml)).ToList();
        }

        private async Task<IReadOnlyCollection<IOEntry>> ListBlobsAsync(
           IOPath path, bool recurse,
           CancellationToken cancellationToken) {
            var result = new List<IOEntry>();

            // maxResults default is 5'000

            path.ExtractPrefixAndRelativePath(out string containerName, out IOPath prefix);

            string rawXml = await ListBlobsAsync(containerName,
                prefix.IsRootPath ? null : prefix.Full.Trim('/') + "/",
               delimiter: recurse ? null : "/");

            XElement x = XElement.Parse(rawXml);
            XElement? blobs = x.Element("Blobs");
            if(blobs != null) {
                result.AddRange(ConvertBlobBatch(blobs));
            }

            XElement? nextMarker = x.Element("NextMarker");

            if(recurse) {
                Implicits.AssumeImplicitFolders(prefix, result);
            }

            return result;
        }

        public override Task<Stream> OpenWrite(IOPath path, CancellationToken cancellationToken = default) {
            return OpenWrite(path, false, cancellationToken);
        }

        public Task<Stream> OpenAppend(IOPath path, CancellationToken cancellationToken = default) {
            return OpenWrite(path, true, cancellationToken);
        }


        private Task<Stream> OpenWrite(IOPath path, bool append, CancellationToken cancellationToken = default) {
            if(path is null)
                throw new ArgumentNullException(nameof(path));

            return Task.FromResult<Stream>(new AzureWriteStream(this, path, append));
        }

        private HttpRequestMessage CreatePutBlockRequest(int blockId, IOPath path, byte[] buffer, int count, out string blockIdStr) {
            blockIdStr = Convert.ToBase64String(Encoding.ASCII.GetBytes(blockId.ToString("d6")));

            // call https://docs.microsoft.com/en-us/rest/api/storageservices/put-block
            var request = new HttpRequestMessage(HttpMethod.Put, $"{path.NLS}?comp=block&blockid={blockIdStr}");
            request.Content = new ByteArrayContent(buffer, 0, count);
            return request;
        }

        private HttpRequestMessage CreateAppendBlockRequest(IOPath path, byte[] buffer, int count) {
            // call https://docs.microsoft.com/en-us/rest/api/storageservices/append-block
            var request = new HttpRequestMessage(HttpMethod.Put, $"{path.NLS}?comp=appendblock");
            request.Content = new ByteArrayContent(buffer, 0, count);
            return request;
        }

        public string PutBlock(int blockId, IOPath path, byte[] buffer, int count) {
            HttpRequestMessage request = CreatePutBlockRequest(blockId, path, buffer, count, out string blockIdStr);
            HttpResponseMessage response = Send(request);
            response.EnsureSuccessStatusCode();
            return blockIdStr;
        }


        public void AppendBlock(IOPath path, byte[] buffer, int count) {
            HttpRequestMessage request = CreateAppendBlockRequest(path, buffer, count);
            HttpResponseMessage response = Send(request);
            if(response.StatusCode == HttpStatusCode.NotFound)
                throw new FileNotFoundException();

            // trying to append to block blob results in 409 Conflict error. What you can do is re-make the blob as "append".
            if(response.StatusCode == HttpStatusCode.Conflict) {
            }

            response.EnsureSuccessStatusCode();
        }

        public async Task<string> PutBlockAsync(int blockId, IOPath path, byte[] buffer, int count) {
            HttpRequestMessage request = CreatePutBlockRequest(blockId, path, buffer, count, out string blockIdStr);
            HttpResponseMessage response = await SendAsync(request);
            response.EnsureSuccessStatusCode();
            return blockIdStr;
        }

        public async Task AppendBlockAsync(IOPath path, byte[] buffer, int count) {
            HttpRequestMessage request = CreateAppendBlockRequest(path, buffer, count);
            HttpResponseMessage response = await SendAsync(request);
            if(response.StatusCode == HttpStatusCode.NotFound)
                throw new FileNotFoundException();
            response.EnsureSuccessStatusCode();
        }

        private HttpRequestMessage CreatePutBlockListRequest(IOPath path, IEnumerable<string> blockIds) {
            /* sample body
               Request Body:  
               <?xml version="1.0" encoding="utf-8"?>  
               <BlockList>  
               <Latest>AAAAAA==</Latest>  
               <Latest>AQAAAA==</Latest>  
               <Latest>AZAAAA==</Latest>  
               </BlockList> */

            var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?><BlockList>");
            foreach(string id in blockIds) {
                sb.Append("<Latest>").Append(id).Append("</Latest>");
            }
            sb.Append("</BlockList>");

            // call https://docs.microsoft.com/en-us/rest/api/storageservices/put-block-list
            var request = new HttpRequestMessage(HttpMethod.Put, $"{path.NLS}?comp=blocklist");
            request.Content = new StringContent(sb.ToString());
            return request;
        }

        public void PutBlockList(IOPath path, IEnumerable<string> blockIds) {
            HttpResponseMessage response = Send(CreatePutBlockListRequest(path, blockIds));
            response.EnsureSuccessStatusCode();
        }

        public async Task PutBlockListAsync(IOPath path, IEnumerable<string> blockIds) {
            HttpResponseMessage response = await SendAsync(CreatePutBlockListRequest(path, blockIds));
            response.EnsureSuccessStatusCode();
        }

        private HttpRequestMessage CreatePutBlobRequest(IOPath path, string blobType) {
            // https://docs.microsoft.com/en-us/rest/api/storageservices/put-blob

            var request = new HttpRequestMessage(HttpMethod.Put, path.NLS);
            request.Content = new ByteArrayContent(new byte[0]);  // sets Content-Type to 0
            request.Headers.Add("x-ms-blob-type", blobType);
            return request;
        }


        public async Task PutBlobAsync(IOPath path, string blobType = "BlockBlob") {
            HttpResponseMessage response = await SendAsync(CreatePutBlobRequest(path, blobType));
            response.EnsureSuccessStatusCode();
        }

        public void PutBlob(IOPath path, string blobType = "BlockBlob") {
            HttpResponseMessage response = Send(CreatePutBlobRequest(path, blobType));
            response.EnsureSuccessStatusCode();
        }

        public override async Task<Stream?> OpenRead(IOPath path, CancellationToken cancellationToken = default) {
            if(path is null)
                throw new ArgumentNullException(nameof(path));

            // call https://docs.microsoft.com/en-us/rest/api/storageservices/get-blob
            var request = new HttpRequestMessage(HttpMethod.Get, path.NLS);
            HttpResponseMessage response = await SendAsync(request);

            if(response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }

        public override async Task Rm(IOPath path, CancellationToken cancellationToken = default) {
            if(path is null)
                throw new ArgumentNullException(nameof(path));

            if(path.IsFolder) {
                await RmRecurseWithLs(path, cancellationToken);
            } else {
                // https://docs.microsoft.com/en-us/rest/api/storageservices/delete-blob
                HttpResponseMessage response = await SendAsync(new HttpRequestMessage(HttpMethod.Delete, path));
                if(response.StatusCode != HttpStatusCode.NotFound) {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public override async Task<IOEntry?> Stat(IOPath path, CancellationToken cancellationToken = default) {
            if(path is null)
                throw new ArgumentNullException(nameof(path));

            // call https://docs.microsoft.com/en-us/rest/api/storageservices/get-blob
            var request = new HttpRequestMessage(HttpMethod.Head, path.NLS);
            HttpResponseMessage response = await SendAsync(request);

            if(response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            // response does not have a body:
            // https://learn.microsoft.com/en-us/rest/api/storageservices/get-blob-properties?tabs=microsoft-entra-id#response

            string? creationTime = response.GetHeaderValue("x-ms-creation-time");


            // todo:
            // - x-ms-meta-name:value
            // - x-ms-tag-count
            //

            var e = new IOEntry(path) {
                CreatedTime = creationTime == null ? null : DateTimeOffset.Parse(creationTime),
                LastModificationTime = response.Content.Headers.LastModified,
                Size = response.Content.Headers.ContentLength,
                MD5 = response.Content.Headers.ContentMD5.ToHexString() ?? string.Empty
            };

            e.TryAddProperties(
                "BlobType", response.GetHeaderValue("x-ms-blob-type"),
                "LeaseState", response.GetHeaderValue("x-ms-lease-state"),
                "LeaseDuration", response.GetHeaderValue("x-ms-lease-duration"),
                "ContentType", response.Content.Headers.ContentType,
                "ETag", response.Headers.ETag,
                "ContentEncoding", response.Content.Headers.ContentEncoding,
                "ContentLanguage", response.Content.Headers.ContentLanguage,
                "ContentDisposition", response.Content.Headers.ContentDisposition,
                "CacheControl", response.Headers.CacheControl,
                "ServerEncrypted", response.GetHeaderValue("x-ms-server-encrypted"),
                "EncryptionKeySha256", response.GetHeaderValue("x-ms-encryption-key-sha256"),
                "EncryptionContext", response.GetHeaderValue("x-ms-encryption-context"),
                "EncryptionScope", response.GetHeaderValue("x-ms-encryption-scope"),
                "AccessTier", response.GetHeaderValue("x-ms-access-tier"),
                "AccessTierChangeTime", response.GetHeaderValue("x-ms-access-tier-change-time"),
                "ArchiveStatus", response.GetHeaderValue("x-ms-archive-status"),
                "RehydratePriority", response.GetHeaderValue("x-ms-rehydrate-priority"),
                "LastAccessTime", response.GetHeaderValue("x-ms-last-access-time"),
                "LegalHold", response.GetHeaderValue("x-ms-legal-hold"),
                "Owner", response.GetHeaderValue("x-ms-owner"),
                "Group", response.GetHeaderValue("x-ms-group"),
                "Permissions", response.GetHeaderValue("x-ms-permissions"),
                "ResourceType", response.GetHeaderValue("x-ms-resource-type"),
                "Snapshot", response.GetHeaderValue("x-ms-snapshot"),
                "VersionId", response.GetHeaderValue("x-ms-version-id"),
                "ExpiryTime", response.GetHeaderValue("x-ms-expiry-time"));

            return e;
        }

        private static void PrependContainerName(IReadOnlyCollection<IOEntry> entries, string containerName) {
            foreach(IOEntry entry in entries) {
                entry.Path = entry.Path.Prefix(containerName)!;
            }
        }

    }
}