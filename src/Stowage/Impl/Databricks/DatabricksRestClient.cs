using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// general: https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/
   /// DBFS: https://docs.databricks.com/dev-tools/api/latest/dbfs.html
   /// </summary>
   sealed class DatabricksRestClient
        : PolyfilledHttpFileStorage, IDatabricksClient
   {
      private readonly string _base;
      private readonly string _dbfsBase;
      private readonly string _sqlBase;

      public DatabricksRestClient(Uri instanceUri, string token) : base(instanceUri, new StaticAuthHandler(token))
      {
         _base = instanceUri.ToString().TrimEnd('/') + "/api/2.0";
         _dbfsBase = _base + "/dbfs";
         _sqlBase = _base + "/preview/sql";
      }

      /// <summary>
      /// https://docs.databricks.com/dev-tools/api/latest/dbfs.html#list
      /// </summary>
      /// <param name="path"></param>
      /// <param name="recurse"></param>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      public override async Task<IReadOnlyCollection<IOEntry>> Ls(IOPath path = null, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path != null && !path.IsFolder)
            throw new ArgumentException("path needs to be a folder", nameof(path));

         var result = new List<IOEntry>();
         await Ls(path, result, recurse);
         return result;
      }

      private async Task Ls(IOPath path, List<IOEntry> container, bool recurse)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_dbfsBase}/list");
         request.Content = new StringContent(JsonSerializer.Serialize(new ListRequest { Path = path ?? IOPath.Root }));
         HttpResponseMessage response = await SendAsync(request);
         if(response.StatusCode == HttpStatusCode.NotFound)
            return;
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         ListResponse lr = JsonSerializer.Deserialize<ListResponse>(rjson);

         if((lr?.Files?.Length ?? 0) == 0)
            return;

         var batch = lr.Files.Select(fi => new IOEntry(fi.IsDir ? fi.Path + "/" : fi.Path) { Size = fi.IsDir ? null : fi.FileSize }).ToList();
         container.AddRange(batch);

         if(recurse)
         {
            foreach(IOEntry folder in batch.Where(e => e.Path.IsFolder))
            {
               await Ls(folder.Path, container, recurse);
            }
         }
      }

      public override async Task<Stream> OpenRead(IOPath path, CancellationToken cancellationToken = default)
      {
         // get status to determine length
         FileInfo fi = await Dbfs<GetStatusRequest, FileInfo>
            ("get-status",
            new GetStatusRequest { Path = path ?? IOPath.Root },
            HttpMethod.Get);
         if(fi == null)
            return null;

         //
         return new ReadStream(this, path, fi.FileSize);
      }

      private HttpRequestMessage CreateReadRequest(IOPath path, long offset, long count)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_dbfsBase}/read");
         request.Content = new StringContent(JsonSerializer.Serialize(new ReadRequest { Path = path, Offset = offset, Count = count }));
         return request;
      }

      // https://docs.databricks.com/dev-tools/api/latest/dbfs.html#read
      public byte[] Read(IOPath path, long offset, long count)
      {
         HttpResponseMessage response = Send(CreateReadRequest(path, offset, count));
         response.EnsureSuccessStatusCode();
         ReadResponse rr = JsonSerializer.Deserialize<ReadResponse>(response.Content.ReadAsStringAsync().Result);
         return Convert.FromBase64String(rr.Base64EncodedData);
      }

      public async Task<byte[]> ReadAsync(IOPath path, long offset, long count)
      {
         HttpResponseMessage response = await SendAsync(CreateReadRequest(path, offset, count));
         response.EnsureSuccessStatusCode();
         ReadResponse rr = JsonSerializer.Deserialize<ReadResponse>(await response.Content.ReadAsStringAsync());
         return Convert.FromBase64String(rr.Base64EncodedData);
      }

      public override async Task<Stream> OpenWrite(IOPath path, WriteMode mode, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         //create upload first: https://docs.databricks.com/dev-tools/api/latest/dbfs.html#create
         CreateResponse cr = await Dbfs<CreateRequest, CreateResponse>("create", new CreateRequest { Path = path, Overwrite = true });

         return new WriteStream(this, cr.Handle);
      }

      private HttpRequestMessage CreateAddBlockRequest(long handle, byte[] buffer, int count)
      {
         // https://docs.databricks.com/dev-tools/api/latest/dbfs.html#add-block
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_dbfsBase}/add-block");
         request.Content = new StringContent(JsonSerializer.Serialize(
            new AddBlockRequest { Handle = handle, Base64Data = Convert.ToBase64String(buffer, 0, count) }));
         return request;
      }

      public void AddBlock(long handle, byte[] buffer, int count)
      {
         Send(CreateAddBlockRequest(handle, buffer, count)).EnsureSuccessStatusCode();
      }

      public async Task AddBlockAsync(long handle, byte[] buffer, int count)
      {
         (await SendAsync(CreateAddBlockRequest(handle, buffer, count))).EnsureSuccessStatusCode();
      }

      private HttpRequestMessage CreateCloseRequest(long handle)
      {
         // https://docs.databricks.com/dev-tools/api/latest/dbfs.html#close
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_dbfsBase}/close");
         request.Content = new StringContent(JsonSerializer.Serialize(new CloseRequest { Handle = handle }));
         return request;
      }

      public void Close(long handle)
      {
         Send(CreateCloseRequest(handle)).EnsureSuccessStatusCode();
      }

      public async Task CloseAsync(long handle)
      {
         (await SendAsync(CreateCloseRequest(handle))).EnsureSuccessStatusCode();
      }


      public override async Task Rm(IOPath path, bool recurse = false, CancellationToken cancellationToken = default)
      {
         if(path is null)
            throw new ArgumentNullException(nameof(path));

         // https://docs.databricks.com/dev-tools/api/latest/dbfs.html#delete

         await Dbfs<DeleteRequest, object>("delete", new DeleteRequest { Path = path, Recursive = recurse });
      }

      private async Task<TResponse> Dbfs<TRequest, TResponse>(string command, TRequest request, HttpMethod method = null)
      {
         var httpRequest = new HttpRequestMessage(method ?? HttpMethod.Post, $"{_dbfsBase}/{command}");
         httpRequest.Content = new StringContent(JsonSerializer.Serialize(request));
         HttpResponseMessage httpResponse = await SendAsync(httpRequest);

         if(httpResponse.StatusCode == HttpStatusCode.BadRequest)
         {
            ErrorResponse error = JsonSerializer.Deserialize<ErrorResponse>(await httpResponse.Content.ReadAsStringAsync());
            throw new IOException($"DBFS '{command}' operation failed with code {error?.Code}. Message: {error?.Message}");
         }
         else if(httpResponse.StatusCode == HttpStatusCode.NotFound)
         {
            return default(TResponse);
         }

         httpResponse.EnsureSuccessStatusCode();

         if(typeof(TResponse) == typeof(object))
            return default(TResponse);

         return JsonSerializer.Deserialize<TResponse>(await httpResponse.Content.ReadAsStringAsync());
      }

      public async Task<IReadOnlyCollection<Job>> ListAllJobs(bool includeRuns)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_base}/jobs/list");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();
         JobListResponse jobListResponse = JsonSerializer.Deserialize<JobListResponse>(rjson);

         if((jobListResponse?.Jobs?.Length ?? 0) == 0)
            return new List<Job>();

         if(includeRuns)
         {
            RunsListResponse[] listOfListOfRuns = await Task.WhenAll(jobListResponse.Jobs.Select(rj => ListJobRuns(rj.Id, 2)));
            return jobListResponse.Jobs.Zip(listOfListOfRuns, (job, runs) =>
            {
               if(runs?.Runs != null)
               {
                  job.Runs.AddRange(runs.Runs);
               }
               return job;
            }).ToList();
         }

         return jobListResponse.Jobs.ToList();
      }

      public async Task<Job> LoadJob(long jobId)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_base}/jobs/get?job_id={jobId}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         Job r = JsonSerializer.Deserialize<Job>(rjson);
         RunsListResponse runs = await ListJobRuns(jobId, 100);
         r.Runs.AddRange(runs.Runs);
         return r;
      }

      public async Task RunJobNow(long jobId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_base}/jobs/run-now");
         request.Content = new StringContent($"{{\"job_id\":{jobId}}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
      }

      public async Task CancelRun(long runId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_base}/jobs/runs/cancel");
         request.Content = new StringContent($"{{\"run_id\":{runId}}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
      }


      public async Task<RunsListResponse> ListJobRuns(long jobId, int limit)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_base}/jobs/runs/list?job_id={jobId}&limit={limit}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         return JsonSerializer.Deserialize<RunsListResponse>(rjson);
      }

      public async Task<IReadOnlyCollection<ClusterInfo>> ListAllClusters()
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_base}/clusters/list");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         ClustersListReponse lst = JsonSerializer.Deserialize<ClustersListReponse>(rjson);

         return (lst == null || lst.Clusters == null || lst.Clusters.Length == 0)
            ? new ClusterInfo[0]
            : lst.Clusters.Where(i => i.Source == "UI" || i.Source == "API").ToArray();

      }

      public async Task<ClusterInfo> LoadCluster(string clusterId)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_base}/clusters/get?cluster_id={clusterId}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         return JsonSerializer.Deserialize<ClusterInfo>(rjson);
      }

      public async Task StartCluster(string clusterId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_base}/clusters/start");
         request.Content = new StringContent($"{{\"cluster_id\":\"{clusterId}\"}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
      }

      public async Task RestartCluster(string clusterId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_base}/clusters/restart");
         request.Content = new StringContent($"{{\"cluster_id\":\"{clusterId}\"}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();

      }

      public async Task TerminateCluster(string clusterId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_base}/clusters/delete");
         request.Content = new StringContent($"{{\"cluster_id\":\"{clusterId}\"}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
      }

      public async Task<IReadOnlyCollection<ClusterEvent>> ListClusterEvents(string clusterId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_base}/clusters/events");
         request.Content = new StringContent($"{{\"cluster_id\":\"{clusterId}\"}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         EventsListReponse evts = JsonSerializer.Deserialize<EventsListReponse>(rjson);

         return evts.Events;
      }

      public async Task<IReadOnlyCollection<ObjectInfo>> WorkspaceLs(IOPath path)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_base}/workspace/list");
         request.Content = new StringContent(JsonSerializer.Serialize(new WorkspaceLsRequest { Path = path }));
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         WorkspaceLsResponse objs = JsonSerializer.Deserialize<WorkspaceLsResponse>(rjson);
         return objs.Objects;
      }

      public async Task<IReadOnlyCollection<SqlQueryBase>> ListSqlQueries()
      {
         const long pageSize = 25;
         long pageNo = 0;
         var result = new List<SqlQueryBase>();
         long totalCount;
         do
         {
            (IReadOnlyCollection<SqlQueryBase> queries, long totalCount1) = await ListSqlQueries(pageNo++, pageSize);
            totalCount = totalCount1;
            result.AddRange(queries);
         }
         while(result.Count < totalCount);

         return result;
      }

      private async Task<Tuple<IReadOnlyCollection<SqlQueryBase>, long>> ListSqlQueries(long pageNo, long pageSize)
      {
         // https://redocly.github.io/redoc/?url=https://docs.microsoft.com/azure/databricks/_static/api-refs/queries-dashboards-2.0-azure.yaml#operation/sql-analytics-get-queries

         // pages are 1 - based, not 0 like normal people!
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_sqlBase}/queries?page={pageNo + 1}&page_size={pageSize}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         ListSqlQueriesResponse r = JsonSerializer.Deserialize<ListSqlQueriesResponse>(rjson);

         return new Tuple<IReadOnlyCollection<SqlQueryBase>, long>(r.Results, r.Count);
      }

      public async Task<string> GetSqlQueryRaw(string queryId)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_sqlBase}/queries/{queryId}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();
         return rjson;
      }

      public async Task<SqlQuery> GetSqlQuery(string queryId)
      {
         SqlQuery r = JsonSerializer.Deserialize<SqlQuery>(await GetSqlQueryRaw(queryId));

         return r;
      }

      public async Task UpdateSqlQueryRaw(string queryId, string rawJson)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_sqlBase}/queries/{queryId}");
         request.Content = new StringContent(rawJson);
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
      }

      public async Task<string> CreateSqlQueryRaw(string rawJson)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_sqlBase}/queries");
         request.Content = new StringContent(rawJson);
         HttpResponseMessage response = await SendAsync(request);
         await EnsureSuccessOrThrow(response);

         string rjson = await response.Content.ReadAsStringAsync();
         SqlQueryBase result = JsonSerializer.Deserialize<SqlQueryBase>(rjson);
         return result.Id;
      }


      public async Task<IReadOnlyCollection<AclEntry>> GetSqlQueryAcl(string queryId)
      {
         // see https://redocly.github.io/redoc/?url=https://docs.microsoft.com/azure/databricks/_static/api-refs/queries-dashboards-2.0-azure.yaml#operation/get-sql-analytics-object-permissions

         var request = new HttpRequestMessage(HttpMethod.Get, $"{_sqlBase}/permissions/queries/{queryId}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         GetAclResponse r = JsonSerializer.Deserialize<GetAclResponse>(rjson);

         return r.Acl;
      }

      public async Task SetSqlQueryAcl(string queryId, IEnumerable<AclEntry> acl)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_sqlBase}/permissions/queries/{queryId}");
         string jacl = JsonSerializer.Serialize(acl);
         request.Content = new StringContent(jacl);
         HttpResponseMessage response = await SendAsync(request);
         await EnsureSuccessOrThrow(response);
      }

      public async Task TransferQueryOwnership(string queryId, string newOwnerEmail)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_sqlBase}/permissions/query/{queryId}/transfer");
         request.Content = new StringContent($"{{\"new_owner\": \"{newOwnerEmail}\"}}");
         HttpResponseMessage response = await SendAsync(request);
         await EnsureSuccessOrThrow(response);
      }


      #region [ utility response classes ]

      private async Task EnsureSuccessOrThrow(HttpResponseMessage response)
      {
         if(response.IsSuccessStatusCode)
            return;

         // read response body
         string body = await response.Content.ReadAsStringAsync();
         ErrorResponse jem = JsonSerializer.Deserialize<ErrorResponse>(body);

         throw new HttpRequestException(
            $"request failed with code {(int)response.StatusCode} '{response.StatusCode}'. {jem.Message}.");
      }

      public class GetAclResponse
      {
         [JsonPropertyName("access_control_list")]
         public AclEntry[] Acl { get; set; }
      }

      public class ListSqlQueriesResponse
      {
         [JsonPropertyName("count")]
         public long Count { get; set; }

         [JsonPropertyName("results")]
         public SqlQueryBase[] Results { get; set; }
      }

      public class WorkspaceLsRequest
      {
         [JsonPropertyName("path")]
         public string Path { get; set; }
      }

      public class WorkspaceLsResponse
      {
         [JsonPropertyName("objects")]
         public ObjectInfo[] Objects { get; set; }
      }

      public class JobListResponse
      {
         [JsonPropertyName("jobs")]
         public Job[] Jobs { get; set; }
      }

      public class RunsListResponse
      {
         [JsonPropertyName("runs")]
         public Run[] Runs { get; set; }
      }

      public class ClustersListReponse
      {
         [JsonPropertyName("clusters")]
         public ClusterInfo[] Clusters { get; set; }
      }

      public class EventsListReponse
      {
         [JsonPropertyName("events")]
         public ClusterEvent[] Events { get; set; }
      }

      #endregion
   }
}
