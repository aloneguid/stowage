using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Stowage
{
   abstract class PolyfilledHttpFileStorage : PolyfilledFileStorage
   {
      private readonly HttpClient _http;

      protected PolyfilledHttpFileStorage(Uri baseAddress, DelegatingHandler authHandler)
      {
         _http = new HttpClient(authHandler)
         {
            BaseAddress = baseAddress,
            DefaultRequestHeaders = {
               {
                  "User-Agent",
                  Constants.UserAgent
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

      protected async Task<T> PostAsync<T>(string url,
         object content = null,
         string stringBody = null,
         string contentType = null,
         bool throwOnError = true)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, url);

         if(content != null)
         {
            string rawJson = JsonSerializer.Serialize(content, content.GetType());
            request.Content = new StringContent(rawJson);
         }

         if(stringBody != null)
         {
            request.Content = new StringContent(stringBody, null, contentType);
         }

         HttpResponseMessage response = await SendAsync(request);
         if(throwOnError)
         {
            response.EnsureSuccessStatusCode();
         }

         string jsonString = await response.Content.ReadAsStringAsync();
         return JsonSerializer.Deserialize<T>(jsonString);
      }

      protected async Task<T> GetAsync<T>(HttpRequestMessage request, bool throwOnError = true)
      {
         HttpResponseMessage response = await SendAsync(request);
         if(throwOnError)
         {
            response.EnsureSuccessStatusCode();
         }

         string jsonString = await response.Content.ReadAsStringAsync();
         return JsonSerializer.Deserialize<T>(jsonString);
      }

      protected async Task<T> GetAsync<T>(string url, bool throwOnError = true)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, url);

         return await GetAsync<T>(request, throwOnError);
      }

      public override void Dispose()
      {
         _http.Dispose();

         base.Dispose();
      }
   }
}
