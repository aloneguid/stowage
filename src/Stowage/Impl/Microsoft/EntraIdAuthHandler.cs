using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Microsoft {

    public class ClientSecretCredential {

        public ClientSecretCredential(string tenantId, string clientId, string clientSecret) {
            TenantId = tenantId;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public string TenantId { get; init; }

        public string ClientId { get; init; }

        public string ClientSecret { get; init; }
    }

    class EntraIdAuthHandler : DelegatingHandler {
        private const string AzureCliClientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
        private const string Scope = "https://storage.azure.com//.default";

        private readonly HttpClient _authClient = new HttpClient();
        private readonly ClientSecretCredential _clientSecretCredential;
        private TokenResponse? _token;

        class TokenResponse {
            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public int? ExpiresIn { get; set; }

            [JsonPropertyName("ext_expires_in")]
            public int? ExtExpiresIn { get; set; }

            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }
        }

        public EntraIdAuthHandler(ClientSecretCredential clientSecretCredential) : base(new HttpClientHandler()) {
            _clientSecretCredential = clientSecretCredential;
        }

        private async Task ObtainToken() {
            string tokenRequestUrl = $"https://login.microsoftonline.com/{_clientSecretCredential.TenantId}/oauth2/v2.0/token";
            var nvp = new List<KeyValuePair<string, string>>();
            nvp.Add(new KeyValuePair<string, string>("client_id", _clientSecretCredential.ClientId));
            nvp.Add(new KeyValuePair<string, string>("scope", Scope));
            nvp.Add(new KeyValuePair<string, string>("client_secret", _clientSecretCredential.ClientSecret));
            nvp.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));

            var request = new HttpRequestMessage(HttpMethod.Post, tokenRequestUrl) {
                Content = new FormUrlEncodedContent(nvp)
            };

            HttpResponseMessage response = await _authClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            string jsonText = await response.Content.ReadAsStringAsync();
            _token = JsonSerializer.Deserialize<TokenResponse>(jsonText);
        }

        private async Task Authenticate(HttpRequestMessage request) {
            request.Headers.Add("x-ms-version", Azure.ServiceVersion);

            if(_token == null) {
                await ObtainToken();
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token!.AccessToken);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            await Authenticate(request);

            return await base.SendAsync(request, cancellationToken);
        }

#if NET5_0_OR_GREATER
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) {
            return SendAsync(request, cancellationToken).Result;
        }
#endif
    }
}
