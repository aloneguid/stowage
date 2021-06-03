using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl
{
   sealed class StaticAuthHandler : DelegatingHandler
   {
      private readonly AuthenticationHeaderValue _hv;

      public StaticAuthHandler(string bearerToken) : base(new HttpClientHandler())
      {
         _hv = new AuthenticationHeaderValue("Bearer", bearerToken);
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         request.Headers.Authorization = _hv;
         return base.SendAsync(request, cancellationToken);
      }

#if NET5_0_OR_GREATER
      protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         request.Headers.Authorization = _hv;
         return base.Send(request, cancellationToken);
      }
#endif
   }
}
