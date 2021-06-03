using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Google
{
   /// <summary>
   /// auth: https://cloud.google.com/storage/docs/json_api/v1/how-tos/authorizing
   /// </summary>
   class GoogleAuthHandler : DelegatingHandler
   {
      private readonly GoogleCredential _credential;
      private string _token = null;

      /// <summary>Unix epoch as a <c>DateTime</c></summary>
      public GoogleAuthHandler(GoogleCredential credential) : base(new HttpClientHandler())
      {
         _credential = credential ?? throw new ArgumentNullException(nameof(credential));
      }

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         if(_token == null)
         {
            _token = await _credential.RequestAccessTokenAsync(cancellationToken);
         }

         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

         return await base.SendAsync(request, cancellationToken);
      }

#if NET5_0_OR_GREATER
      protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         return SendAsync(request, cancellationToken).Result;
      }
#endif

   }
}
