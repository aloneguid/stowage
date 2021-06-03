using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Stowage
{
   abstract class PolyfilledHttpFileStorage : PolyfilledFileStorage
   {
      private readonly HttpClient _http;

      protected PolyfilledHttpFileStorage(Uri baseAddress, DelegatingHandler authHandler)
      {
         var fv = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

         _http = new HttpClient(authHandler)
         {
            BaseAddress = baseAddress,
            DefaultRequestHeaders = {
               {
                  "User-Agent",
                  string.Format(Constants.UserAgent, fv.FileMajorPart, fv.FileMinorPart, fv.FileBuildPart)
               }
            }
         };
      }

      protected Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
      {
         return _http.SendAsync(request);
      }

      protected HttpResponseMessage Send(HttpRequestMessage request)
      {
#if NET5_0_OR_GREATER
         return _http.Send(request);
#else
         return _http.SendAsync(request).Result;
#endif
      }

      public override void Dispose()
      {
         _http.Dispose();

         base.Dispose();
      }
   }
}
