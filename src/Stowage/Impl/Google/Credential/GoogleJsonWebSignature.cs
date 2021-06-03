using System.Text.Json.Serialization;

namespace Stowage.Impl.Google.Credential
{
   class GoogleJsonWebSignature
   {
      /// <summary>
      /// The payload as specified in 
      /// https://developers.google.com/accounts/docs/OAuth2ServiceAccount#formingclaimset,
      /// https://developers.google.com/identity/protocols/OpenIDConnect, and
      /// https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims
      /// </summary>
      public class Payload : JsonWebToken.Payload
      {
         /// <summary>
         /// A space-delimited list of the permissions the application requests or <c>null</c>.
         /// </summary>
         [JsonPropertyName("scope")]
         public string Scope { get; set; }

         /// <summary>
         /// The email address of the user for which the application is requesting delegated access.
         /// </summary>
         [JsonPropertyName("prn")]
         public string Prn { get; set; }

         /// <summary>
         /// The hosted GSuite domain of the user. Provided only if the user belongs to a hosted domain.
         /// </summary>
         [JsonPropertyName("hd")]
         public string HostedDomain { get; set; }

         /// <summary>
         /// The user's email address. This may not be unique and is not suitable for use as a primary key.
         /// Provided only if your scope included the string "email".
         /// </summary>
         [JsonPropertyName("email")]
         public string Email { get; set; }

         /// <summary>
         /// True if the user's e-mail address has been verified; otherwise false.
         /// </summary>
         [JsonPropertyName("email_verified")]
         public bool EmailVerified { get; set; }

         /// <summary>
         /// The user's full name, in a displayable form. Might be provided when:
         /// (1) The request scope included the string "profile"; or
         /// (2) The ID token is returned from a token refresh.
         /// When name claims are present, you can use them to update your app's user records.
         /// Note that this claim is never guaranteed to be present.
         /// </summary>
         [JsonPropertyName("name")]
         public string Name { get; set; }

         /// <summary>
         /// Given name(s) or first name(s) of the End-User. Note that in some cultures, people can have multiple given names;
         /// all can be present, with the names being separated by space characters.
         /// </summary>
         [JsonPropertyName("given_name")]
         public string GivenName { get; set; }

         /// <summary>
         /// Surname(s) or last name(s) of the End-User. Note that in some cultures,
         /// people can have multiple family names or no family name;
         /// all can be present, with the names being separated by space characters.
         /// </summary>
         [JsonPropertyName("family_name")]
         public string FamilyName { get; set; }

         /// <summary>
         /// The URL of the user's profile picture. Might be provided when:
         /// (1) The request scope included the string "profile"; or
         /// (2) The ID token is returned from a token refresh.
         /// When picture claims are present, you can use them to update your app's user records.
         /// Note that this claim is never guaranteed to be present.
         /// </summary>
         [JsonPropertyName("picture")]
         public string Picture { get; set; }

         /// <summary>
         /// End-User's locale, represented as a BCP47 [RFC5646] language tag.
         /// This is typically an ISO 639-1 Alpha-2 [ISO639‑1] language code in lowercase and an
         /// ISO 3166-1 Alpha-2 [ISO3166‑1] country code in uppercase, separated by a dash.
         /// For example, en-US or fr-CA.
         /// </summary>
         [JsonPropertyName("locale")]
         public string Locale { get; set; }
      }
   }
}
