using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Microsoft {
    class SasAuthHandler : DelegatingHandler {
        private readonly string _sasToken;

        /// <summary>
        /// Examples: https://docs.microsoft.com/en-us/rest/api/storageservices/service-sas-examples#blob-examples
        /// </summary>
        /// <param name="sasToken">See https://docs.microsoft.com/en-us/azure/storage/common/storage-sas-overview#sas-token</param>
        public SasAuthHandler(string sasToken) : base(new HttpClientHandler()) {
            // make sure they didn't include the leading ?, we should strip it
            if(sasToken.StartsWith('?')) {
                sasToken = sasToken.Substring(1);
            }

            _sasToken = sasToken;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            //?sv=2019-12-12&ss=b&srt=sco&sp=r&se=2021-01-09T19:55:19Z&st=2020-12-07T11:55:19Z&spr=https&sig=ubKzO2g6eWX4pELoKk3XzmptRBi5P34AJj6i42%2Bm5%2FA%3D

            Authenticate(request);

            return base.SendAsync(request, cancellationToken);
        }

#if NET5_0_OR_GREATER
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) {
            Authenticate(request);

            return base.Send(request, cancellationToken);
        }
#endif

        private void Authenticate(HttpRequestMessage request) {
            // azure just requires us to append the sasToken to the url, we just need to check if we already have query arguments or not
            request.RequestUri = new Uri(request.RequestUri + (request.RequestUri.Query.Length > 0 ? "&" : "?") + _sasToken);
        }
    }
}