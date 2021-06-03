using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Google.Credential
{
   class JsonWebSignature
   {
      /// <summary>
      /// Header as specified in http://tools.ietf.org/html/draft-ietf-jose-json-web-signature-11#section-4.1.
      /// </summary>
      public class Header : JsonWebToken.Header
      {
         /// <summary>
         /// Gets or set the algorithm header parameter that identifies the cryptographic algorithm used to secure 
         /// the JWS or <c>null</c>.
         /// </summary>
         [JsonPropertyName("alg")]
         public string Algorithm { get; set; }

         /// <summary>
         /// Gets or sets the JSON Web Key URL header parameter that is an absolute URL that refers to a resource 
         /// for a set of JSON-encoded public keys, one of which corresponds to the key that was used to digitally 
         /// sign the JWS or <c>null</c>.
         /// </summary>
         [JsonPropertyName("jku")]
         public string JwkUrl { get; set; }

         /// <summary>
         /// Gets or sets JSON Web Key header parameter that is a public key that corresponds to the key used to 
         /// digitally sign the JWS or <c>null</c>.
         /// </summary>
         [JsonPropertyName("jwk")]
         public string Jwk { get; set; }

         /// <summary>
         /// Gets or sets key ID header parameter that is a hint indicating which specific key owned by the signer 
         /// should be used to validate the digital signature or <c>null</c>.
         /// </summary>
         [JsonPropertyName("kid")]
         public string KeyId { get; set; }

         /// <summary>
         /// Gets or sets X.509 URL header parameter that is an absolute URL that refers to a resource for the X.509
         /// public key certificate or certificate chain corresponding to the key used to digitally sign the JWS or 
         /// <c>null</c>.
         /// </summary>
         [JsonPropertyName("x5u")]
         public string X509Url { get; set; }

         /// <summary>
         /// Gets or sets X.509 certificate thumb print header parameter that provides a base64url encoded SHA-1 
         /// thumb-print (a.k.a. digest) of the DER encoding of an X.509 certificate that can be used to match the 
         /// certificate or <c>null</c>.
         /// </summary>
         [JsonPropertyName("x5t")]
         public string X509Thumbprint { get; set; }

         /// <summary>
         /// Gets or sets X.509 certificate chain header parameter contains the X.509 public key certificate or 
         /// certificate chain corresponding to the key used to digitally sign the JWS or <c>null</c>.
         /// </summary>
         [JsonPropertyName("x5c")]
         public string X509Certificate { get; set; }

         /// <summary>
         /// Gets or sets array listing the header parameter names that define extensions that are used in the JWS 
         /// header that MUST be understood and processed or <c>null</c>.
         /// </summary>
         [JsonPropertyName("crit")]
         public IList<string> critical { get; set; }
      }
   }
}
