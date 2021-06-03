using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Google.Credential
{
   class JsonWebToken
   {
      /// <summary>
      /// JWT Header as specified in http://tools.ietf.org/html/draft-ietf-oauth-json-web-token-08#section-5.
      /// </summary>
      public class Header
      {
         /// <summary>
         /// Gets or sets type header parameter used to declare the type of this object or <c>null</c>.
         /// </summary>
         [JsonPropertyName("typ")]
         public string Type { get; set; }

         /// <summary>
         /// Gets or sets content type header parameter used to declare structural information about the JWT or 
         /// <c>null</c>.
         /// </summary>
         [JsonPropertyName("cty")]
         public string ContentType { get; set; }
      }

      /// <summary>
      /// JWT Payload as specified in http://tools.ietf.org/html/draft-ietf-oauth-json-web-token-08#section-4.1.
      /// </summary>
      public class Payload
      {
         /// <summary>
         /// Gets or sets issuer claim that identifies the principal that issued the JWT or <c>null</c>.
         /// </summary>
         [JsonPropertyName("iss")]
         public string Issuer { get; set; }

         /// <summary>
         /// Gets or sets subject claim identifying the principal that is the subject of the JWT or <c>null</c>.
         /// </summary>
         [JsonPropertyName("sub")]
         public string Subject { get; set; }

         /// <summary>
         /// Gets or sets audience claim that identifies the audience that the JWT is intended for (should either be
         /// a string or list) or <c>null</c>.
         /// </summary>
         [JsonPropertyName("aud")]
         public object Audience { get; set; }

         /// <summary>
         /// Gets or sets the target audience claim that identifies the audience that an OIDC token generated from
         /// this JWT is intended for. Maybe be null. Multiple target audiences are not supported.
         /// <c>null</c>.
         /// </summary>
         [JsonPropertyName("target_audience")]
         public string TargetAudience { get; set; }

         /// <summary>
         /// Gets or sets expiration time claim that identifies the expiration time (in seconds) on or after which 
         /// the token MUST NOT be accepted for processing or <c>null</c>.
         /// </summary>
         [JsonPropertyName("exp")]
         public long? ExpirationTimeSeconds { get; set; }

         /// <summary>
         /// Gets or sets not before claim that identifies the time (in seconds) before which the token MUST NOT be
         /// accepted for processing or <c>null</c>.
         /// </summary>
         [JsonPropertyName("nbf")]
         public long? NotBeforeTimeSeconds { get; set; }

         /// <summary>
         /// Gets or sets issued at claim that identifies the time (in seconds) at which the JWT was issued or 
         /// <c>null</c>.
         /// </summary>
         [JsonPropertyName("iat")]
         public long? IssuedAtTimeSeconds { get; set; }

         /// <summary>
         /// Gets or sets JWT ID claim that provides a unique identifier for the JWT or <c>null</c>.
         /// </summary>
         [JsonPropertyName("jti")]
         public string JwtId { get; set; }

         /// <summary>
         /// The nonce value specified by the client during the authorization request.
         /// Must be present if a nonce was specified in the authorization request, otherwise this will not be present.
         /// </summary>
         [JsonPropertyName("nonce")]
         public string Nonce { get; set; }

         /// <summary>
         /// Gets or sets type claim that is used to declare a type for the contents of this JWT Claims Set or 
         /// <c>null</c>.
         /// </summary>
         [JsonPropertyName("typ")]
         public string Type { get; set; }

         /// <summary>Gets the audience property as a list.</summary>
         [JsonIgnore]
         public IEnumerable<string> AudienceAsList
         {
            get
            {
               var asList = Audience as List<string>;
               if(asList != null)
               {
                  return asList;
               }
               var list = new List<string>();
               string asString = Audience as string;
               if(asString != null)
               {
                  list.Add(asString);
               }

               return list;
            }
         }

         [JsonIgnore]
         internal DateTimeOffset? IssuedAt =>
             IssuedAtTimeSeconds is null ? (DateTimeOffset?)null : GoogleCredential.UnixEpoch.AddSeconds(IssuedAtTimeSeconds.Value);

         [JsonIgnore]
         internal DateTimeOffset? ExpiresAt =>
             ExpirationTimeSeconds is null ? (DateTimeOffset?)null : GoogleCredential.UnixEpoch.AddSeconds(ExpirationTimeSeconds.Value);
      }
   }
}
