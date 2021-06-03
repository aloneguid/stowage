using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Stowage.Impl.Amazon
{
   /// <summary>
   /// https://docs.aws.amazon.com/AmazonS3/latest/API/sigv4-auth-using-authorization-header.html
   /// 
   /// explore python version: https://github.com/tedder/requests-aws4auth
   /// </summary>
   class S3AuthHandler : DelegatingHandler
   {
      private readonly string _accessKeyId;
      private readonly string _secretAccessKey;
      private readonly string _region;
      private readonly string _service;
      private static readonly string EmptySha256 = new byte[0].SHA256().ToHexString();

      public S3AuthHandler(string accessKeyId, string secretAccessKey, string region, string service = "s3") : base(new HttpClientHandler())
      {
         _accessKeyId = accessKeyId;
         _secretAccessKey = secretAccessKey;
         _region = region;
         _service = service;
      }

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         await SignAsync(request);

         return await base.SendAsync(request, cancellationToken);
      }

#if NET5_0_OR_GREATER
      protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         return SendAsync(request, cancellationToken).Result;
      }
#endif

      protected async Task<string> SignAsync(HttpRequestMessage request, DateTimeOffset? signDate = null)
      {
         // a very helpful article on S3 auth: https://docs.aws.amazon.com/AmazonS3/latest/API/sig-v4-header-based-auth.html

         DateTimeOffset dateToUse = signDate ?? DateTimeOffset.UtcNow;
         string nowDate = dateToUse.ToString("yyyyMMdd");
         string amzNowDate = GetAmzDate(dateToUse);

         request.Headers.Add("x-amz-date", amzNowDate);

         // 1. Create a canonical request

         /*
          * <HTTPMethod>\n
          * <CanonicalURI>\n
          * <CanonicalQueryString>\n
          * <CanonicalHeaders>\n
          * <SignedHeaders>\n
          * <HashedPayload>
          */

         string payloadHash = await AddPayloadHashHeader(request);

         string canonicalRequest = request.Method + "\n" +
            GetCanonicalUri(request) + "\n" +  // CanonicalURI
            GetCanonicalQueryString(request) + "\n" +
            GetCanonicalHeaders(request, out string signedHeaders) + "\n" +   // ends up with two newlines which is expected
            signedHeaders + "\n" +
            payloadHash;

#if DEBUG
         Debug.WriteLine("canonical request: " + canonicalRequest);
#endif


         // 2. Create a string to sign

         // step by step instructions: https://docs.aws.amazon.com/general/latest/gr/sigv4-create-string-to-sign.html

         /*
          * StringToSign =
          *    Algorithm + \n +
          *    RequestDateTime + \n +
          *    CredentialScope + \n +
          *    HashedCanonicalRequest
          */

         string stringToSign = "AWS4-HMAC-SHA256\n" +
               amzNowDate + "\n" +
               nowDate + "/" + _region + "/s3/aws4_request\n" +
               canonicalRequest.SHA256();


         // 3. Calculate Signature

         /*
          * DateKey              = HMAC-SHA256("AWS4"+"<SecretAccessKey>", "<YYYYMMDD>")
          * DateRegionKey        = HMAC-SHA256(<DateKey>, "<aws-region>")
          * DateRegionServiceKey = HMAC-SHA256(<DateRegionKey>, "<aws-service>")
          * SigningKey           = HMAC-SHA256(<DateRegionServiceKey>, "aws4_request")
          */

         byte[] kSecret = Encoding.UTF8.GetBytes(("AWS4" + _secretAccessKey).ToCharArray());
         byte[] kDate = HmacSha256(nowDate, kSecret);
         byte[] kRegion = HmacSha256(_region, kDate);
         byte[] kService = HmacSha256(_service, kRegion);
         byte[] kSigning = HmacSha256("aws4_request", kService);

         // final signature
         byte[] signatureRaw = HmacSha256(stringToSign, kSigning);
         string signature = signatureRaw.ToHexString();

         string auth = $"Credential={_accessKeyId}/{nowDate}/{_region}/s3/aws4_request,SignedHeaders={signedHeaders},Signature={signature}";
         request.Headers.Authorization = new AuthenticationHeaderValue("AWS4-HMAC-SHA256", auth);

         return signature;
      }

      private static string GetAmzDate(DateTimeOffset date)
      {
         return date.ToString("yyyyMMddTHHmmssZ");
      }

      private string GetCanonicalUri(HttpRequestMessage request)
      {
         string path = request.RequestUri.AbsolutePath;
         string[] ppts = IOPath.Split(path);

         return IOPath.Combine(ppts.Select(p => p.UrlEncode()));
      }

      private string GetCanonicalQueryString(HttpRequestMessage request)
      {
         /**
          * CanonicalQueryString specifies the URI-encoded query string parameters. You URI-encode name and values individually. You must also sort the parameters in the canonical query string alphabetically by key name. The sorting occurs after encoding.
          */


         NameValueCollection values = HttpUtility.ParseQueryString(request.RequestUri.Query);
         var sb = new StringBuilder();

         // a. Sort the parameter names by character code point in ascending order. Parameters with duplicate names should be sorted by value.For example, a parameter name that begins with the uppercase letter F precedes a parameter name that begins with a lowercase letter b.

         foreach(string key in values.AllKeys.OrderBy(k => k))
         {
            if(sb.Length > 0)
            {
               sb.Append('&');
            }

            // URI-encode each parameter name and value.
            // Do not URI-encode any of the unreserved characters that RFC 3986 defines: A-Z, a-z, 0-9, hyphen ( - ), underscore ( _ ), period ( . ), and tilde ( ~ ).
            string value = values[key].UrlEncode();

            if(key == null)
            {
               sb
                  .Append(value)
                  .Append("=");
            }
            else
            {
               sb
                  .Append(key.UrlEncode())
                  .Append("=")
                  .Append(value);

            }
         }

         return sb.ToString();
      }

      private string GetCanonicalHeaders(HttpRequestMessage request, out string signedHeaders)
      {
         // List of request headers with their values.
         // Individual header name and value pairs are separated by the newline character ("\n").
         // Header names must be in lowercase. You must sort the header names alphabetically to construct the string.

         // Note that I add some headers manually, but preserve sorting order in the actual code.

         var headers = from kvp in request.Headers
                       where kvp.Key.StartsWith("x-amz-", StringComparison.OrdinalIgnoreCase)
                       orderby kvp.Key
                       select new { Key = kvp.Key.ToLowerInvariant(), kvp.Value };

         var sb = new StringBuilder();
         var signedHeadersList = new List<string>();

         // The CanonicalHeaders list must include the following:
         // - HTTP host header.
         // - If the Content-Type header is present in the request, you must add it to the CanonicalHeaders list.
         // - Any x-amz-* headers that you plan to include in your request must also be added. For example, if you are using temporary security credentials, you need to include x-amz-security-token in your request. You must add this header in the list of CanonicalHeaders.

         string contentType = request.Content?.Headers.ContentType?.ToString();
         if(contentType != null)
         {
            sb.Append("content-type:").Append(contentType).Append("\n");
            signedHeadersList.Add("content-type");
         }

         if(request.Headers.Contains("date"))
         {
            sb.Append("date:").Append(request.Headers.GetValues("date").First()).Append("\n");
            signedHeadersList.Add("date");
         }

         sb.Append("host:").Append(request.RequestUri.Host).Append("\n");
         signedHeadersList.Add("host");

         if(request.Headers.Contains("range"))
         {
            sb.Append("range:").Append(request.Headers.GetValues("range").First()).Append("\n");
            signedHeadersList.Add("range");
         }

         // Create the string in the right format; this is what makes the headers "canonicalized" --
         //   it means put in a standard format. http://en.wikipedia.org/wiki/Canonicalization
         foreach(var kvp in headers)
         {
            sb.Append(kvp.Key).Append(":");
            signedHeadersList.Add(kvp.Key);

            foreach(string hv in kvp.Value)
            {
               sb.Append(hv);
            }

            sb.Append("\n");
         }

         signedHeaders = string.Join(";", signedHeadersList);

         return sb.ToString();
      }

      /// <summary>
      /// Hex(SHA256Hash(<payload>))
      /// </summary>
      /// <param name="request"></param>
      /// <returns></returns>
      private async Task<string> AddPayloadHashHeader(HttpRequestMessage request)
      {
         string hash;

         if(request.Content != null)
         {
            byte[] content = await request.Content.ReadAsByteArrayAsync();
            hash = content.SHA256().ToHexString();
         }
         else
         {
            hash = EmptySha256;
         }

         request.Headers.Add("x-amz-content-sha256", hash);

         return hash;
      }

      private static byte[] HmacSha256(string data, byte[] key)
      {
         var alg = KeyedHashAlgorithm.Create("HmacSHA256");
         alg.Key = key;
         return alg.ComputeHash(Encoding.UTF8.GetBytes(data));
      }
   }
}
