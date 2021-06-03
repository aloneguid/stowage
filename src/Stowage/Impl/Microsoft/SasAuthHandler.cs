using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Stowage.Impl.Microsoft
{
   internal class SasAuthHandler : DelegatingHandler
   {
      private readonly string _signedStart;
      private readonly string _signedExpiry;
      private readonly string _signedResource;
      private readonly string _signedPermissions;


      /// <summary>
      /// Examples: https://docs.microsoft.com/en-us/rest/api/storageservices/service-sas-examples#blob-examples
      /// </summary>
      /// <param name="sasToken">See https://docs.microsoft.com/en-us/azure/storage/common/storage-sas-overview#sas-token</param>
      public SasAuthHandler(string sasToken)
      {
         // parse SAS token into parts to sign later
         NameValueCollection qs = HttpUtility.ParseQueryString(sasToken);
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         //?sv=2019-12-12&ss=b&srt=sco&sp=r&se=2021-01-09T19:55:19Z&st=2020-12-07T11:55:19Z&spr=https&sig=ubKzO2g6eWX4pELoKk3XzmptRBi5P34AJj6i42%2Bm5%2FA%3D

         return base.SendAsync(request, cancellationToken);
      }
   }
}
