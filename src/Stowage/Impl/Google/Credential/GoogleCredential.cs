using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Stowage.Impl.Google.Credential;

namespace Stowage.Impl.Google
{
   class GoogleCredential
   {
      const string TokenServerUrl = "https://oauth2.googleapis.com/token";
      public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      private static readonly HttpClient _http = new HttpClient();

      public GoogleCredential(JsonCredentialParameters j, RSA key)
      {
         J = j;
         Key = key;
      }

      public JsonCredentialParameters J { get; }

      public RSA Key { get; }

      public string[] Scopes { get; } = { "https://www.googleapis.com/auth/devstorage.full_control" };

      public static GoogleCredential FromJson(string json)
      {
         JsonCredentialParameters p = JsonSerializer.Deserialize<JsonCredentialParameters>(json);

         switch(p.Type)
         {
            case "service_account":
               return CreateServiceAccountCredentialFromParameters(p);
            default:
               throw new InvalidOperationException($"Error creating credential from JSON. Unrecognized credential type {p.Type}.");
         }
      }


      private static GoogleCredential CreateServiceAccountCredentialFromParameters(JsonCredentialParameters j)
      {
         if(j.Type != "service_account" ||
            string.IsNullOrEmpty(j.ClientEmail) ||
            string.IsNullOrEmpty(j.PrivateKey))
         {
            throw new InvalidOperationException("JSON data does not represent a valid service account credential.");
         }

         RSAParameters parameters = Pkcs8.DecodeRsaParameters(j.PrivateKey);
         var key = RSA.Create();
         key.ImportParameters(parameters);

         return new GoogleCredential(j, key);

      }

      /// <summary>
      /// Creates a claim set as specified in
      /// https://developers.google.com/accounts/docs/OAuth2ServiceAccount#formingclaimset.
      /// </summary>
      private GoogleJsonWebSignature.Payload CreatePayload()
      {
         int totalSeconds = (int)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
         GoogleJsonWebSignature.Payload payload = new GoogleJsonWebSignature.Payload();
         payload.Issuer = J.ClientEmail;
         payload.Audience = TokenServerUrl;
         payload.IssuedAtTimeSeconds = new long?(totalSeconds);
         payload.ExpirationTimeSeconds = new long?(totalSeconds + 3600);
         payload.Subject = null;
         payload.Scope = string.Join(" ", this.Scopes);
         return payload;
      }

      /// <summary>
      /// Creates a serialized header as specified in
      /// https://developers.google.com/accounts/docs/OAuth2ServiceAccount#formingheader.
      /// </summary>
      private string CreateSerializedHeader()
      {
         JsonWebSignature.Header header = new JsonWebSignature.Header();
         header.Algorithm = "RS256";
         header.Type = "JWT";
         header.KeyId = J.PrivateKeyId;
         return JsonSerializer.Serialize(header);
      }

      /// <summary>
      /// Creates a base64 encoded signature for the SHA-256 hash of the specified data.
      /// </summary>
      /// <param name="data">The data to hash and sign. Must not be null.</param>
      /// <returns>The base-64 encoded signature.</returns>
      public string CreateSignature(byte[] data)
      {
         using(SHA256 shA256 = SHA256.Create())
            return Convert.ToBase64String(Key.SignHash(shA256.ComputeHash(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
      }

      /// <summary>
      /// Signs JWT token using the private key and returns the serialized assertion.
      /// </summary>
      /// <param name="payload">the JWT payload to sign.</param>
      private string CreateAssertion(GoogleJsonWebSignature.Payload payload)
      {
         string serializedHeader = CreateSerializedHeader();
         string str = JsonSerializer.Serialize(payload);

         StringBuilder stringBuilder = new StringBuilder();
         stringBuilder.Append(TokenEncodingHelpers.UrlSafeBase64Encode(serializedHeader)).Append('.').Append(TokenEncodingHelpers.UrlSafeBase64Encode(str));
         string signature = this.CreateSignature(Encoding.ASCII.GetBytes(stringBuilder.ToString()));
         stringBuilder.Append('.').Append(TokenEncodingHelpers.UrlSafeEncode(signature));
         return stringBuilder.ToString();
      }

      /// <summary>
      /// Requests a new token as specified in
      /// https://developers.google.com/accounts/docs/OAuth2ServiceAccount#makingrequest.
      /// </summary>
      /// <param name="taskCancellationToken">Cancellation token to cancel operation.</param>
      /// <returns><c>true</c> if a new token was received successfully.</returns>
      public async Task<string> RequestAccessTokenAsync(CancellationToken taskCancellationToken)
      {
         string assertion = CreateAssertion(CreatePayload());

         var request = new HttpRequestMessage(HttpMethod.Post, TokenServerUrl);
         request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
         {
            ["assertion"] = assertion,
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer"
         });
         HttpResponseMessage response = await _http.SendAsync(request);
         response.EnsureSuccessStatusCode();
         string json = await response.Content.ReadAsStringAsync();
         TokenResponse tr = JsonSerializer.Deserialize<TokenResponse>(json);

         return tr.AccessToken;
      }
   }


}
