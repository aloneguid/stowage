using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Google.Credential
{
   /// <summary>
   /// OAuth 2.0 model for a successful access token response as specified in 
   /// http://tools.ietf.org/html/rfc6749#section-5.1.
   /// </summary>
   public class TokenResponse
   {
      private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

      // Internal for testing.
      // Refresh token 6 minutes before it expires.
      internal const int TokenRefreshTimeWindowSeconds = 60 * 6;
      // Don't use a token within 5 minutes of it actually expiring.
      internal const int TokenHardExpiryTimeWindowSeconds = 60 * 5;

      /// <summary>Gets or sets the access token issued by the authorization server.</summary>
      [JsonPropertyName("access_token")]
      public string AccessToken { get; set; }

      /// <summary>
      /// Gets or sets the token type as specified in http://tools.ietf.org/html/rfc6749#section-7.1.
      /// </summary>
      [JsonPropertyName("token_type")]
      public string TokenType { get; set; }

      /// <summary>Gets or sets the lifetime in seconds of the access token.</summary>
      [JsonPropertyName("expires_in")]
      public long? ExpiresInSeconds { get; set; }

      /// <summary>
      /// Gets or sets the refresh token which can be used to obtain a new access token.
      /// For example, the value "3600" denotes that the access token will expire in one hour from the time the 
      /// response was generated.
      /// </summary>
      [JsonPropertyName("refresh_token")]
      public string RefreshToken { get; set; }

      /// <summary>
      /// Gets or sets the scope of the access token as specified in http://tools.ietf.org/html/rfc6749#section-3.3.
      /// </summary>
      [JsonPropertyName("scope")]
      public string Scope { get; set; }

      /// <summary>
      /// Gets or sets the id_token, which is a JSON Web Token (JWT) as specified in http://tools.ietf.org/html/draft-ietf-oauth-json-web-token
      /// </summary>
      [JsonPropertyName("id_token")]
      public string IdToken { get; set; }
   }
}