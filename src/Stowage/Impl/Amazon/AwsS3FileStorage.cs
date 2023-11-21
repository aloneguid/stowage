using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Amazon {
    sealed class AwsS3FileStorage : PolyfilledHttpFileStorage {
        private readonly XmlResponseParser _xmlParser = new XmlResponseParser();
        private readonly IOPath? _prefix;
        private readonly bool _supportsMultiPartUpload;

        public AwsS3FileStorage(Uri endpoint, DelegatingHandler authHandler, IOPath? prefix = null, bool supportsMultiPartUpload = true) :
           base(endpoint, authHandler) {
            _prefix = prefix;
            _supportsMultiPartUpload = supportsMultiPartUpload;
        }

        private IOPath? GetFullPath(IOPath? path) {
            if(path == null)
                return null;
            return _prefix == null ? path : path.Prefix(_prefix);
        }

        public override async Task<IReadOnlyCollection<IOEntry>> Ls(IOPath relPath, bool recurse = false, CancellationToken cancellationToken = default) {

            IOPath? path = GetFullPath(relPath);

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

                HttpResponseMessage response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, uri));
                response.EnsureSuccessStatusCode();
                string xml = await response.Content.ReadAsStringAsync();

                IReadOnlyCollection<IOEntry> page = _xmlParser.ParseListObjectV2Response(xml, out continuationToken);
                result.AddRange(page);
            }
            while(continuationToken != null);

            if(recurse) {
                Implicits.AssumeImplicitFolders(path, result);
            }

            return result;
        }

        public override async Task Rm(IOPath? relPath, bool recurse, CancellationToken cancellationToken = default) {
            if(relPath is null)
                throw new ArgumentNullException(nameof(relPath));

            IOPath path = GetFullPath(relPath);

            // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_DeleteObject.html
            (await SendAsync(
               new HttpRequestMessage(HttpMethod.Delete, path.NLS)))
               .EnsureSuccessStatusCode();
        }

        public override void Dispose() {

        }

        public override async Task<Stream?> OpenRead(IOPath relPath, CancellationToken cancellationToken = default) {
            if(relPath is null)
                throw new ArgumentNullException(nameof(relPath));

            IOPath? path = GetFullPath(relPath);

            // call https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetObject.html
            HttpResponseMessage response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{IOPath.Normalize(path, true)}"));

            if(response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }

        public override async Task<Stream> OpenWrite(IOPath? relPath, CancellationToken cancellationToken = default) {
            if(relPath is null)
                throw new ArgumentNullException(nameof(relPath));

            IOPath? path = GetFullPath(relPath);
            string npath = IOPath.Normalize(path, true);

            if(_supportsMultiPartUpload) {
                // initiate upload and get upload ID
                var request = new HttpRequestMessage(HttpMethod.Post, $"/{npath}?uploads");
                HttpResponseMessage response = await SendAsync(request);
                response.EnsureSuccessStatusCode();
                string xml = await response.Content.ReadAsStringAsync(); // this contains UploadId
                string uploadId = _xmlParser.ParseInitiateMultipartUploadResponse(xml);

                return new AwsMultiPartWriteStream(this, npath, uploadId);
            }

            return new AwsWriteStream(this, npath);
        }

        // https://docs.aws.amazon.com/AmazonS3/latest/API/API_UploadPart.html
        private HttpRequestMessage CreateUploadPartRequest(string key, string uploadId, int partNumber, byte[] buffer, int count) {
            var request = new HttpRequestMessage(HttpMethod.Put, $"{key}?partNumber={partNumber}&uploadId={uploadId}");
            request.Content = new ByteArrayContent(buffer, 0, count);
            return request;
        }

        public string UploadPart(string key, string uploadId, int partNumber, byte[] buffer, int count) {
            HttpResponseMessage response = Send(CreateUploadPartRequest(key, uploadId, partNumber, buffer, count));
            response.EnsureSuccessStatusCode();
            return response.Headers.GetValues("ETag").First();
        }

        public async Task<string> UploadPartAsync(string key, string uploadId, int partNumber, byte[] buffer, int count) {
            HttpResponseMessage response = await SendAsync(CreateUploadPartRequest(key, uploadId, partNumber, buffer, count));
            response.EnsureSuccessStatusCode();
            return response.Headers.GetValues("ETag").First();
        }

        //https://docs.aws.amazon.com/AmazonS3/latest/API/API_CompleteMultipartUpload.html
        private HttpRequestMessage CreateCompleteMultipartUploadRequest(string key, string uploadId, IEnumerable<string> partTags) {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{key}?uploadId={uploadId}");

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

        public void CompleteMultipartUpload(string key, string uploadId, IEnumerable<string> partTags) {
            Send(CreateCompleteMultipartUploadRequest(key, uploadId, partTags)).EnsureSuccessStatusCode();
        }

        public async Task CompleteMultipartUploadAsync(string key, string uploadId, IEnumerable<string> partTags) {
            (await SendAsync(CreateCompleteMultipartUploadRequest(key, uploadId, partTags))).EnsureSuccessStatusCode();
        }

        public void CompleteUpload(string key, Stream content) {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/{key}") { Content = new StreamContent(content) };
            Send(request).EnsureSuccessStatusCode();
        }

        public async Task CompleteUploadAsync(string key, Stream content) {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/{key}") { Content = new StreamContent(content) };
            (await SendAsync(request)).EnsureSuccessStatusCode();
        }
    }
}
