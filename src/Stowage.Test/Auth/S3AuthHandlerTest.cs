using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Stowage.Impl.Amazon;
using Xunit;

namespace Stowage.Test.Auth {
    class AuthHandlerWrapper : S3AuthHandler {
        public AuthHandlerWrapper(string accessKeyId, string secretAccessKey, string region) : base(accessKeyId, secretAccessKey, region) {
        }

        public async Task<HttpRequestMessage> Exec(HttpMethod method, string url, DateTimeOffset date, Dictionary<string, string> extraHeaders = null,
           byte[] body = null) {
            var request = new HttpRequestMessage(method, url);

            if(body != null) {
                request.Content = new ByteArrayContent(body);
            }

            if(extraHeaders != null) {
                foreach(KeyValuePair<string, string> entry in extraHeaders) {
                    request.Headers.Add(entry.Key, entry.Value);
                }
            }

            await SignAsync(request, date);

            return request;
        }
    }

    public class S3AuthHandlerTest {

        private void CheckHeader(HttpRequestMessage message, string headerName, string expectedValue) {
            Assert.True(message.Headers.Contains(headerName), $"missing header {headerName}");

            Assert.Equal(expectedValue, message.Headers.GetValues(headerName).First());
        }

        /// <summary>
        /// Taken from example at https://docs.aws.amazon.com/AmazonS3/latest/API/sig-v4-header-based-auth.html
        /// </summary>
        [Fact]
        public async Task Get_first_10_bytes_of_a_file_in_a_bucket() {
            string keyId = "AKIAIOSFODNN7EXAMPLE";
            string secret = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

            var handler = new AuthHandlerWrapper(keyId, secret, "us-east-1");
            HttpRequestMessage message = await handler.Exec(HttpMethod.Get, $"https://examplebucket.s3.amazonaws.com/test.txt",
               new DateTime(2013, 5, 24),
               new Dictionary<string, string> {
                   ["Range"] = "bytes=0-9"
               });

            CheckHeader(message, "x-amz-date", "20130524T000000Z");
            CheckHeader(message, "x-amz-content-sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
            CheckHeader(message, "Authorization", "AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request,SignedHeaders=host;range;x-amz-content-sha256;x-amz-date,Signature=f0e8bdb87c964420e857bd35b5d6ed310bd44f0170aba48dd91039c6036bdb41");
        }

        [Fact]
        public async Task Put_object() {
            string keyId = "AKIAIOSFODNN7EXAMPLE";
            string secret = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

            var handler = new AuthHandlerWrapper(keyId, secret, "us-east-1");

            HttpRequestMessage message = await handler.Exec(HttpMethod.Put, $"https://examplebucket.s3.amazonaws.com/test$file.text",
               new DateTime(2013, 5, 24),
               new Dictionary<string, string> {
                   ["date"] = "Fri, 24 May 2013 00:00:00 GMT",
                   ["x-amz-storage-class"] = "REDUCED_REDUNDANCY"
               },
               "Welcome to Amazon S3.".UTF8Bytes());

            CheckHeader(message, "x-amz-date", "20130524T000000Z");
            CheckHeader(message, "x-amz-content-sha256", "44ce7dd67c959e0d3524ffac1771dfbba87d2b6b4b4e99e42034a8b803f8b072");
            CheckHeader(message, "Authorization", "AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request,SignedHeaders=date;host;x-amz-content-sha256;x-amz-date;x-amz-storage-class,Signature=b34099c0e352465cff1a5957d94a2c97473337c0dc30aa3ce793435a81c6ecf4");
        }

        [Fact]
        public async Task Get_object_from_custom_endpoint() {
            string keyId = "AKIAIOSFODNN7EXAMPLE";
            string secret = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

            var handler = new AuthHandlerWrapper(keyId, secret, "us-east-1");
            HttpRequestMessage message = await handler.Exec(HttpMethod.Get, $"https://examplebucket.myprovider.com:9000/test.txt",
               new DateTime(2013, 5, 24));

            CheckHeader(message, "x-amz-date", "20130524T000000Z");
            CheckHeader(message, "x-amz-content-sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
            CheckHeader(message, "Authorization", "AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request,SignedHeaders=host;x-amz-content-sha256;x-amz-date,Signature=2244d60dfc04dde1f44b8b159357b6972dbe29b93d56b99ebbb89cc2e6c37380");
        }

        [Fact]
        public async Task Delete_folder() {
            string keyId = "AKIAIOSFODNN7EXAMPLE";
            string secret = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

            var handler = new AuthHandlerWrapper(keyId, secret, "us-east-1");
            HttpRequestMessage message = await handler.Exec(HttpMethod.Delete, $"https://examplebucket.s3.amazonaws.com/test/",
               new DateTime(2013, 5, 24));

            CheckHeader(message, "x-amz-date", "20130524T000000Z");
            CheckHeader(message, "x-amz-content-sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
            CheckHeader(message, "Authorization", "AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request,SignedHeaders=host;x-amz-content-sha256;x-amz-date,Signature=3d74a056d3421e65f7357031be0efc8255ac2bd79bcfced92f7414bcd9ffec4a");
        }

        [Fact]
        public async Task Get_bucket_lifecycle() {
            string keyId = "AKIAIOSFODNN7EXAMPLE";
            string secret = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

            var handler = new AuthHandlerWrapper(keyId, secret, "us-east-1");

            HttpRequestMessage message = await handler.Exec(HttpMethod.Get, $"https://examplebucket.s3.amazonaws.com/?lifecycle",
               new DateTime(2013, 5, 24));

            CheckHeader(message, "x-amz-date", "20130524T000000Z");
            CheckHeader(message, "x-amz-content-sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
            CheckHeader(message, "Authorization", "AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request,SignedHeaders=host;x-amz-content-sha256;x-amz-date,Signature=fea454ca298b7da1c68078a5d1bdbfbbe0d65c699e0f91ac7a200a0136783543");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task List_objects(bool explicitPort) {
            string keyId = "AKIAIOSFODNN7EXAMPLE";
            string secret = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

            var handler = new AuthHandlerWrapper(keyId, secret, "us-east-1");

            string port = explicitPort ? ":443" : string.Empty;
            HttpRequestMessage message = await handler.Exec(HttpMethod.Get, $"https://examplebucket.s3.amazonaws.com{port}/?max-keys=2&prefix=J",
               new DateTime(2013, 5, 24));
            CheckHeader(message, "x-amz-date", "20130524T000000Z");
            CheckHeader(message, "x-amz-content-sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
            CheckHeader(message, "Authorization", "AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request,SignedHeaders=host;x-amz-content-sha256;x-amz-date,Signature=34b48302e7b5fa45bde8084f4b7868a86f0a534bc59db6670ed5711ef69dc6f7");
        }
    }
}