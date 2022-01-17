using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Stowage.Impl.Microsoft
{
   /// <summary>
   /// Shared Key authentication scheme, described in details: https://docs.microsoft.com/en-us/rest/api/storageservices/authorize-with-shared-key.
   /// Note that for table service there are slight differences: https://docs.microsoft.com/en-us/rest/api/storageservices/authorize-with-shared-key#table-service-shared-key-authorization
   /// </summary>
   internal class SharedKeyAuthHandler : DelegatingHandler
   {
      private const string ServiceVersion = "2019-12-12";
      private const string ODataVersion = "3.0"; // https://docs.microsoft.com/en-us/rest/api/storageservices/setting-the-odata-data-service-version-headers
      private readonly string _accountName;
      private readonly string _sharedKey;
      private readonly bool _useSimpleTableAuth;

      public SharedKeyAuthHandler(string accountName, string sharedKey, bool useSimpleTableAuth = false) : base(new HttpClientHandler())
      {
         _accountName = accountName;
         _sharedKey = sharedKey;
         _useSimpleTableAuth = useSimpleTableAuth;
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         Authenticate(request);

         return base.SendAsync(request, cancellationToken);
      }

#if NET5_0_OR_GREATER
      protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         Authenticate(request);

         return base.Send(request, cancellationToken);
      }
#endif

      private void Authenticate(HttpRequestMessage request)
      {
         //auth

         DateTime now = DateTime.UtcNow;
         string nowHeader = now.ToString("R", CultureInfo.InvariantCulture);
         request.Headers.Add("x-ms-date", nowHeader);
         request.Headers.Add("x-ms-version", ServiceVersion);

         string signature;

         if(_useSimpleTableAuth)
         {
            request.Headers.Add("DataServiceVersion", ODataVersion);
            request.Headers.Add("MaxDataServiceVersion", ODataVersion);
            signature = GetSimpleAuthSignature(request, _accountName, _sharedKey, nowHeader);
         }
         else
         {
            signature = GetAuthSignature(request, _accountName, _sharedKey);
         }

         // any extra headers to be added here, before creating auth header!

         request.Headers.Authorization = new AuthenticationHeaderValue("SharedKey", $"{_accountName}:{signature}");
      }

      /// <summary>
      /// Uses simplified authentication, specific for table services only.
      /// See https://docs.microsoft.com/en-us/rest/api/storageservices/authorize-with-shared-key#table-service-shared-key-authorization
      /// StringToSign = VERB + "\n" +
      ///         Content-MD5 + "\n" +
      ///         Content-Type + "\n" +  
      ///         Date + "\n" +  
      ///         CanonicalizedResource;
      /// </summary>
      private static string GetSimpleAuthSignature(HttpRequestMessage request,
         string accountName, string accountKey,
         string nowHeader,
         string md5 = "")
      {
         // This is the raw representation of the message signature.
         string messageSignature = request.Method + "\n" +  // VERB
            md5 + "\n" +                                    // Content-MD5
            request.Content?.Headers.ContentType + "\n" +   // Content-Type
            nowHeader + "\n" +                              // Date
            GetCanonicalizedResource(request.RequestUri, accountName);


         // Now turn it into a byte array.
         byte[] SignatureBytes = Encoding.UTF8.GetBytes(messageSignature);

         // Create the HMACSHA256 version of the storage key.
         HMACSHA256 SHA256 = new HMACSHA256(Convert.FromBase64String(accountKey));

         // Compute the hash of the SignatureBytes and convert it to a base64 string.
         return Convert.ToBase64String(SHA256.ComputeHash(SignatureBytes));
      }

      private static string GetAuthSignature(HttpRequestMessage request,
         string accountName, string accountKey,
         string ifMatch = "",
         string md5 = "")
      {
         long? contentLength = request.Content?.Headers.ContentLength;
         string scl = (contentLength == null || contentLength == 0) ? string.Empty : contentLength.Value.ToString();

         //todo: sign content-type

         // This is the raw representation of the message signature.
         string messageSignature = request.Method + "\n" +
            "\n" +         // Content-Encoding
            "\n" +         // Content-Language
            scl + "\n" +   // Content-Length
            md5 + "\n" +   // Content-MD5
           request.Content?.Headers.ContentType + "\n" +          // Content-Type
           "\n" +          // Date
           "\n" +          // If-Modified-Since
           ifMatch + "\n" +// If-Match
           "\n" +          // If-None-Match
           "\n" +          // If-Unmodified-Since
           "\n" +          // Range
           GetCanonicalizedHeaders(request) +   // already includes newline at the end
           GetCanonicalizedResource(request.RequestUri, accountName);


         // Now turn it into a byte array.
         byte[] SignatureBytes = Encoding.UTF8.GetBytes(messageSignature);

         // Create the HMACSHA256 version of the storage key.
         HMACSHA256 SHA256 = new HMACSHA256(Convert.FromBase64String(accountKey));

         // Compute the hash of the SignatureBytes and convert it to a base64 string.
         return Convert.ToBase64String(SHA256.ComputeHash(SignatureBytes));
      }

      /// <summary>
      /// Put the headers that start with x-ms in a list and sort them.
      /// Then format them into a string of [key:value\n] values concatenated into one string.
      /// (Canonicalized Headers = headers where the format is standardized).
      /// </summary>
      /// <param name="request">The request that will be made to the storage service.</param>
      /// <returns>Error message; blank if okay.</returns>
      private static string GetCanonicalizedHeaders(HttpRequestMessage request)
      {
         var headers = from kvp in request.Headers
                       where kvp.Key.StartsWith("x-ms-", StringComparison.OrdinalIgnoreCase)
                       orderby kvp.Key
                       select new { Key = kvp.Key.ToLowerInvariant(), kvp.Value };

         StringBuilder sb = new StringBuilder();

         // Create the string in the right format; this is what makes the headers "canonicalized" --
         //   it means put in a standard format. http://en.wikipedia.org/wiki/Canonicalization
         foreach(var kvp in headers)
         {
            StringBuilder headerBuilder = new StringBuilder(kvp.Key);
            char separator = ':';

            // Get the value for each header, strip out \r\n if found, then append it with the key.
            foreach(string headerValues in kvp.Value)
            {
               string trimmedValue = headerValues.TrimStart().Replace("\r\n", String.Empty);
               headerBuilder.Append(separator).Append(trimmedValue);

               // Set this to a comma; this will only be used 
               //   if there are multiple values for one of the headers.
               separator = ',';
            }
            sb.Append(headerBuilder.ToString()).Append("\n");
         }
         return sb.ToString();
      }

      /// <summary>
      /// This part of the signature string represents the storage account 
      ///   targeted by the request. Will also include any additional query parameters/values.
      /// For ListContainers, this will return something like this:
      ///   /storageaccountname/\ncomp:list
      /// </summary>
      /// <param name="address">The URI of the storage service.</param>
      /// <param name="accountName">The storage account name.</param>
      /// <returns>String representing the canonicalized resource.</returns>
      private static string GetCanonicalizedResource(Uri address, string storageAccountName)
      {
         // The absolute path is "/" because for we're getting a list of containers.
         StringBuilder sb = new StringBuilder("/").Append(storageAccountName).Append(address.AbsolutePath);

         // Address.Query is the resource, such as "?comp=list".
         // This ends up with a NameValueCollection with 1 entry having key=comp, value=list.
         // It will have more entries if you have more query parameters.
         NameValueCollection values = HttpUtility.ParseQueryString(address.Query);

         foreach(string item in values.AllKeys.OrderBy(k => k))
         {
            string v = values[item];
            // v = HttpUtility.UrlEncode(v);
            sb.Append('\n').Append(item.ToLower()).Append(':').Append(v);
         }

         return sb.ToString();

      }
   }
}
