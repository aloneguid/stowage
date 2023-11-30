using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Amazon {
    sealed class AwsS3FileStorage : PolyfilledHttpFileStorage, IAwsS3FileStorage {
        private readonly XmlResponseParser _xmlParser = new XmlResponseParser();
        private readonly Uri _endpoint;
        private readonly BucketAddressingStyle _addressingStyle;

        public AwsS3FileStorage(
            Uri endpoint,
            DelegatingHandler authHandler,
            BucketAddressingStyle addressingStyle = BucketAddressingStyle.VirtualHost) :
           base(null, authHandler) {
            _endpoint = endpoint;
            _addressingStyle = addressingStyle;
        }

        private string MakeUrl(string? bucketName, string pathAndQuery) {
            Uri uri;
            if(_addressingStyle == BucketAddressingStyle.VirtualHost) {
                string bucketPart = bucketName == null ? "" : bucketName + ".";
                uri = new Uri($"{_endpoint.Scheme}://{bucketPart}{_endpoint.Authority}");
            } else {
                uri = new Uri($"{_endpoint.Scheme}://{_endpoint.Authority}/{bucketName}/");
            }
            return new Uri(uri, pathAndQuery).ToString();
        }

        public override async Task<IReadOnlyCollection<IOEntry>> Ls(IOPath? path, bool recurse = false, CancellationToken cancellationToken = default) {
            if(path != null && !path.IsFolder)
                throw new ArgumentException("path needs to be a folder (end with '/')", nameof(path));

            // listing root folder is a special case - list buckets
            if(path == null || path.IsRootPath) {
                IReadOnlyCollection<Bucket> buckets = await ListBuckets(cancellationToken);
                return buckets.Select(b => new IOEntry(b.Name + "/") {
                    CreatedTime = b.CreationDate
                }).ToList();
            }

            path.ExtractPrefixAndRelativePath(out string bucketName, out IOPath relativePath);
            return await LsInsideBucket(bucketName, relativePath, recurse, cancellationToken);
        }

        private async Task<IReadOnlyCollection<IOEntry>> LsInsideBucket(string bucketName, IOPath path, bool recurse = false, CancellationToken cancellationToken = default) {

            if(path != null && !path.IsFolder)
                throw new ArgumentException("path needs to be a folder", nameof(path));

            // continuation token example:
            // returned:                             1/HWqxZghDpOj1MLV9DKZjc/6/iobTFl/UaSmTelqKMisHEMdlwDuFMLna2x/slc7p7+HpzLFtVg0QWtMImKepg==
            // in the next query: continuation-token=1%2FHWqxZghDpOj1MLV9DKZjc%2F6%2FiobTFl%2FUaSmTelqKMisHEMdlwDuFMLna2x%2Fslc7p7%2BHpzLFtVg0QWtMImKepg%3D%3D


            string? delimiter = recurse ? null : "/";
            string? prefix = IOPath.IsRoot(path) ? null : path?.NLWTS;
            string? continuationToken = null;
            var result = new List<IOEntry>();

            do {
                // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_ListObjectsV2.html
                string uri = "?list-type=2";
                if(delimiter != null)
                    uri += "&delimiter=" + delimiter;
                if(prefix != null)
                    uri += "&prefix=" + prefix;
                if(continuationToken != null) {
                    // token needs to be amazon-encoded
                    uri += "&continuation-token=" + AWSSDKUtils.UrlEncode(continuationToken, false);
                }

                HttpResponseMessage response = await SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, MakeUrl(bucketName, uri)),
                    true);
                response.EnsureSuccessStatusCode();
                string xml = await response.Content.ReadAsStringAsync();

                IReadOnlyCollection<IOEntry> page = _xmlParser.ParseListObjectV2Response(xml, out continuationToken);
                result.AddRange(page);
            }
            while(continuationToken != null);

            if(recurse) {
                Implicits.AssumeImplicitFolders(path, result);
            }

            // return with prepended bucket name
            PrependBucketName(result, bucketName);
            return result;
        }

        private async Task<IReadOnlyCollection<Bucket>> ListBuckets(CancellationToken cancellationToken = default) {
            // list buckets operation: https://docs.aws.amazon.com/AmazonS3/latest/API/API_ListBuckets.html
            HttpResponseMessage response = await SendAsync(
                new HttpRequestMessage(HttpMethod.Get, MakeUrl(null, "/")), true);
            string xml = await response.Content.ReadAsStringAsync();

            return _xmlParser.ParseListBucketsResponse(xml);
        }

        public override async Task Rm(IOPath? path, CancellationToken cancellationToken = default) {
            if(path is null)
                throw new ArgumentNullException(nameof(path));

            if(path.IsFolder) {
                await RmRecurseWithLs(path, cancellationToken);
            } else {
                // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_DeleteObject.html
                path.ExtractPrefixAndRelativePath(out string bucketName, out IOPath relativePath);
                await SendAsync(
                    new HttpRequestMessage(HttpMethod.Delete, MakeUrl(bucketName, relativePath.NLS)),
                    true);
            }
        }

        public override void Dispose() {

        }

        public override async Task<Stream?> OpenRead(IOPath path, CancellationToken cancellationToken = default) {
            if(path is null)
                throw new ArgumentNullException(nameof(path));

            path.ExtractPrefixAndRelativePath(out string bucketName, out IOPath relativePath);

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                MakeUrl(bucketName, $"{IOPath.Normalize(relativePath, true)}"));

            // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetObject.html
            HttpResponseMessage response = await SendAsync(request);

            if(response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if(!response.IsSuccessStatusCode)
                await ThrowFromResponse(response);

            return await response.Content.ReadAsStreamAsync();
        }

        public async override Task<IOEntry?> Stat(IOPath path, CancellationToken cancellationToken = default) {
            if(path is null)
                throw new ArgumentNullException(nameof(path));

            path.ExtractPrefixAndRelativePath(out string bucketName, out IOPath relativePath);

            // https://docs.aws.amazon.com/AmazonS3/latest/API/API_HeadObject.html
            var request = new HttpRequestMessage(
                HttpMethod.Head,
                MakeUrl(bucketName, $"{IOPath.Normalize(relativePath, true)}"));

            HttpResponseMessage response = await SendAsync(request);
            if(response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if(!response.IsSuccessStatusCode)
                await ThrowFromResponse(response);

            var e = new IOEntry(path) {
                Size = response.Content.Headers.ContentLength,
                LastModificationTime = response.Content.Headers.LastModified
            };
            e.TryAddProperties(
                "ContentType", response.Content.Headers.ContentType?.ToString(),
                "ETag", response.Headers.ETag?.ToString(),
                "Server", response.Headers.Server?.ToString(),
                "ServerSideEncryption", response.GetHeaderValue("x-amz-server-side-encryption"),
                "StorageClass", response.GetHeaderValue("x-amz-storage-class"),
                "VersionId", response.GetHeaderValue("x-amz-version-id"),
                "WebsiteRedirectLocation", response.GetHeaderValue("x-amz-website-redirect-location"));
            return e;
        }

        /// <summary>
        /// Strangely enough, Minio does not support this call, therefore I'm leaving it out for now.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private async Task<IOEntry?> GetObjectAttributes(IOPath path, CancellationToken cancellationToken = default) {
            // https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetObjectAttributes.html

            if(path is null)
                throw new ArgumentNullException(nameof(path));

            path.ExtractPrefixAndRelativePath(out string bucketName, out IOPath relativePath);

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                MakeUrl(bucketName, $"{IOPath.Normalize(relativePath, true)}?attributes"));
            request.Headers.Add("x-amz-object-attributes", "ETag,Checksum,StorageClass,ObjectSize");

            HttpResponseMessage response = await SendAsync(request);

            if(response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if(!response.IsSuccessStatusCode)
                await ThrowFromResponse(response);

            string xml = await response.Content.ReadAsStringAsync();
            IOEntry entry = _xmlParser.ParseGetObjectAttributesResponse(path, xml);
            entry.LastModificationTime = response.Content.Headers.LastModified;
            return entry;
        }

        public override async Task<Stream> OpenWrite(IOPath? path, CancellationToken cancellationToken = default) {
            if(path is null)
                throw new ArgumentNullException(nameof(path));

            path.ExtractPrefixAndRelativePath(out string bucketName, out IOPath relativePath);

            string npath = IOPath.Normalize(relativePath, true);

            // initiate upload and get upload ID
            var request = new HttpRequestMessage(
                HttpMethod.Post, 
                MakeUrl(bucketName, $"{npath}?uploads"));
            HttpResponseMessage response = await SendAsync(request, true);
            string xml = await response.Content.ReadAsStringAsync(); // this contains UploadId
            string? uploadId = _xmlParser.ParseInitiateMultipartUploadResponse(xml);
            if(uploadId == null)
                throw new InvalidOperationException("UploadId not found in response");  

            return new AwsWriteStream(this, bucketName, npath, uploadId);
        }

        // https://docs.aws.amazon.com/AmazonS3/latest/API/API_UploadPart.html
        private HttpRequestMessage CreateUploadPartRequest(string bucketName, string key, string uploadId, int partNumber, byte[] buffer, int count) {
            var request = new HttpRequestMessage(
                HttpMethod.Put,
                MakeUrl(bucketName,
                $"{key}?partNumber={partNumber}&uploadId={uploadId}"));
            request.Content = new ByteArrayContent(buffer, 0, count);
            return request;
        }

        public string UploadPart(string bucketName, string key, string uploadId, int partNumber, byte[] buffer, int count) {
            HttpResponseMessage response = Send(CreateUploadPartRequest(bucketName, key, uploadId, partNumber, buffer, count));
            response.EnsureSuccessStatusCode();
            return response.Headers.GetValues("ETag").First();
        }

        public async Task<string> UploadPartAsync(string bucketName, string key, string uploadId, int partNumber, byte[] buffer, int count) {
            HttpResponseMessage response = await SendAsync(CreateUploadPartRequest(bucketName, key, uploadId, partNumber, buffer, count));
            response.EnsureSuccessStatusCode();
            return response.Headers.GetValues("ETag").First();
        }

        //https://docs.aws.amazon.com/AmazonS3/latest/API/API_CompleteMultipartUpload.html
        private HttpRequestMessage CreateCompleteMultipartUploadRequest(string bucketName, string key, string uploadId, IEnumerable<string> partTags) {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                MakeUrl(bucketName, $"{key}?uploadId={uploadId}"));

            var sb = new StringBuilder(@"<?xml version=""1.0"" encoding=""UTF-8""?><CompleteMultipartUpload xmlns=""http://s3.amazonaws.com/doc/2006-03-01/"">");
            int partId = 1;
            foreach(string eTag in partTags) {
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

        public void CompleteMultipartUpload(string bucketName, string key, string uploadId, IEnumerable<string> partTags) {
            Send(CreateCompleteMultipartUploadRequest(bucketName, key, uploadId, partTags), true);
        }

        public async Task CompleteMultipartUploadAsync(string bucketName, string key, string uploadId, IEnumerable<string> partTags) {
            await SendAsync(CreateCompleteMultipartUploadRequest(bucketName, key, uploadId, partTags), true);
        }

        private static void PrependBucketName(IReadOnlyCollection<IOEntry> entries, string bucketName) {
            foreach(IOEntry entry in entries) {
                entry.Path = entry.Path.Prefix(bucketName)!;
            }
        }   

        #region [ AWS Specific error handling ]

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool expectSuccess) {
            HttpResponseMessage response = await base.SendAsync(request);
            if (expectSuccess) {
                if(!response.IsSuccessStatusCode) {
                    await ThrowFromResponse(response);
                }
            }
            return response;
        }

        private async Task ThrowFromResponse(HttpResponseMessage response) {
            string errorDocument = await response.Content.ReadAsStringAsync();
            // todo: this document can be parsed for more details later
            throw new Exception($"request failed with code {(int)response.StatusCode} ({response.StatusCode}): {errorDocument}");

        }

        private HttpResponseMessage Send(HttpRequestMessage request, bool expectSuccess) {
            HttpResponseMessage response = base.Send(request);
            if(expectSuccess) {
                if(!response.IsSuccessStatusCode) {
                    string errorDocument = response.Content.ReadAsStringAsync().Result;
                    // todo: this document can be parsed for more details later
                    throw new Exception($"request failed with code {(int)response.StatusCode} ({response.StatusCode}): {errorDocument}");
                }
            }
            return response;
        }

        #endregion
    }
}
