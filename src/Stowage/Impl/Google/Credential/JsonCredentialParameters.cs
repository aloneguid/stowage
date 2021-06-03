using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Google.Credential
{
   /// <summary>
   /// Holder for credential parameters read from JSON credential file.
   /// Fields are union of parameters for all supported credential types.
   /// </summary>
   class JsonCredentialParameters
   {
      /// <summary>
      /// UserCredential is created by the GCloud SDK tool when the user runs
      /// <a href="https://cloud.google.com/sdk/gcloud/reference/auth/login">GCloud Auth Login</a>.
      /// </summary>
      public const string AuthorizedUserCredentialType = "authorized_user";

      /// <summary>
      /// ServiceAccountCredential is downloaded by the user from
      /// <a href="https://console.developers.google.com">Google Developers Console</a>.
      /// </summary>
      public const string ServiceAccountCredentialType = "service_account";

      /// <summary>Type of the credential.</summary>
      [JsonPropertyName("type")]
      public string Type { get; set; }

      /// <summary>
      /// Project ID associated with this credential.
      /// </summary>
      [JsonPropertyName("project_id")]
      public string ProjectId { get; set; }

      /// <summary>
      /// Project ID associated with this credential for the purposes
      /// of quota calculations and billing.
      /// </summary>
      [JsonPropertyName("quota_project_id")]
      public string QuotaProject { get; set; }

      /// <summary>
      /// Client Id associated with UserCredential created by
      /// <a href="https://cloud.google.com/sdk/gcloud/reference/auth/login">GCloud Auth Login</a>.
      /// </summary>
      [JsonPropertyName("client_id")]
      public string ClientId { get; set; }

      /// <summary>
      /// Client Secret associated with UserCredential created by
      /// <a href="https://cloud.google.com/sdk/gcloud/reference/auth/login">GCloud Auth Login</a>.
      /// </summary>
      [JsonPropertyName("client_secret")]
      public string ClientSecret { get; set; }

      /// <summary>
      /// Client Email associated with ServiceAccountCredential obtained from
      /// <a href="https://console.developers.google.com">Google Developers Console</a>
      /// </summary>
      [JsonPropertyName("client_email")]
      public string ClientEmail { get; set; }

      /// <summary>
      /// Private Key associated with ServiceAccountCredential obtained from
      /// <a href="https://console.developers.google.com">Google Developers Console</a>.
      /// </summary>
      [JsonPropertyName("private_key")]
      public string PrivateKey { get; set; }

      /// <summary>
      /// Private Key ID associated with ServiceAccountCredential obtained from
      /// <a href="https://console.developers.google.com">Google Developers Console</a>.
      /// </summary>
      [JsonPropertyName("private_key_id")]
      public string PrivateKeyId { get; set; }

      /// <summary>
      /// Refresh Token associated with UserCredential created by
      /// <a href="https://cloud.google.com/sdk/gcloud/reference/auth/login">GCloud Auth Login</a>.
      /// </summary>
      [JsonPropertyName("refresh_token")]
      public string RefreshToken { get; set; }
   }

}
