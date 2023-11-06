﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NetBox.FileFormats.Ini;

namespace Stowage.Impl.Databricks
{
   /// <summary>
   /// general: https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/
   /// DBFS: https://docs.databricks.com/dev-tools/api/latest/dbfs.html
   /// Databricks SQL (Analytics): https://docs.microsoft.com/en-us/azure/databricks/sql/api/
   /// </summary>
   sealed class DatabricksRestClient
        : PolyfilledHttpFileStorage, IDatabricksClient
   {
      private readonly string _apiBase;
      private readonly string _apiBase12;
      private readonly string _apiBase20;
      private readonly string _apiBase21;
      private readonly string _dbfsBase;
      private readonly string _sqlPreviewBase;
      private readonly string _sqlBase;
      private readonly string _scimBase;

      public DatabricksRestClient(string profileName) : this(new Uri(GetProfileHost(profileName)), GetProfileToken(profileName))
      {
      }

      private static string GetProfileHost(string profileName)
      {
         GetProfileHostToken(profileName, out string host, out _);
         return host;
      }

      private static string GetProfileToken(string profileName)
      {
         GetProfileHostToken(profileName, out _, out string token);
         return token;
      }

      private static void GetProfileHostToken(string profileName, out string host, out string token)
      {
         host = Environment.GetEnvironmentVariable("DATABRICKS_HOST");
         token = Environment.GetEnvironmentVariable("DATABRICKS_TOKEN");

         if(host != null)
            return;

         string configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".databrickscfg");
         if(File.Exists(configPath))
         {
            var ini = StructuredIniFile.FromString(File.ReadAllText(configPath));
            host = ini[$"{profileName}.host"];
            token = ini[$"{profileName}.token"];
         }
      }

      public DatabricksRestClient(Uri instanceUri, string token) : base(instanceUri, new StaticAuthHandler(token))
      {
         _apiBase = instanceUri.ToString().TrimEnd('/') + "/api";
         _apiBase12 = _apiBase + "/1.2";
         _apiBase20 = _apiBase + "/2.0";
         _apiBase21 = _apiBase + "/2.1";
         _dbfsBase = _apiBase20 + "/dbfs";
         _sqlPreviewBase = _apiBase20 + "/preview/sql";
         _sqlBase = _apiBase20 + "/sql";
         _scimBase = _apiBase20 + "/preview/scim/v2";
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
         if(path != null && !path.IsFolderPath)
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
         if(response.StatusCode == HttpStatusCode.Forbidden)
            return;
         if(!response.IsSuccessStatusCode)
         {
            throw new IOException($"failed to list path {path}, response code: {response.StatusCode} ({response.ReasonPhrase})");
         }
         string rjson = await response.Content.ReadAsStringAsync();

         ListResponse lr = JsonSerializer.Deserialize<ListResponse>(rjson);

         if((lr?.Files?.Length ?? 0) == 0)
            return;

         var batch = lr?.Files.Select(fi => new IOEntry(fi.IsDir ? fi.Path + "/" : fi.Path) { Size = fi.IsDir ? null : fi.FileSize }).ToList();
         if(batch != null)
         {
            container.AddRange(batch);

            if(recurse)
            {
               foreach(IOEntry folder in batch.Where(e => e.Path.IsFolderPath))
               {
                  await Ls(folder.Path, container, recurse);
               }
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
         return rr == null ? null : Convert.FromBase64String(rr.Base64EncodedData);
      }

      public async Task<byte[]> ReadAsync(IOPath path, long offset, long count)
      {
         HttpResponseMessage response = await SendAsync(CreateReadRequest(path, offset, count));
         response.EnsureSuccessStatusCode();
         ReadResponse rr = JsonSerializer.Deserialize<ReadResponse>(await response.Content.ReadAsStringAsync());
         return rr == null ? null : Convert.FromBase64String(rr.Base64EncodedData);
      }

      public override async Task<Stream> OpenWrite(IOPath path, CancellationToken cancellationToken = default)
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

      public async Task<IReadOnlyCollection<Job>> LsJobs(bool includeRuns)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBase20}/jobs/list");
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

      public async Task<Job> GetJob(long jobId)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBase20}/jobs/get?job_id={jobId}");
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
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase20}/jobs/run-now");
         request.Content = new StringContent($"{{\"job_id\":{jobId}}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
      }

      public async Task CancelJobRun(long runId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase20}/jobs/runs/cancel");
         request.Content = new StringContent($"{{\"run_id\":{runId}}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
      }


      public async Task<RunsListResponse> ListJobRuns(long jobId, int limit)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBase20}/jobs/runs/list?job_id={jobId}&limit={limit}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         return JsonSerializer.Deserialize<RunsListResponse>(rjson);
      }

      public async Task<long> CreateJob(string jobJson, string apiVersion = "2.0")
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase}/{apiVersion}/jobs/create");
         request.Content = new StringContent(jobJson);
         HttpResponseMessage response = await SendAsync(request);
         await EnsureSuccessOrThrow(response);
         string rjson = await response.Content.ReadAsStringAsync();
         return JsonSerializer.Deserialize<CreateJobResponse>(rjson)?.JobId ?? 0;
      }

      public async Task ResetJob(long jobId, string jobJson, string apiVersion = "2.0")
      {
         var requestDict = new Dictionary<string, object>();
         Dictionary<string, object> jobDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jobJson);
         requestDict["job_id"] = jobId;
         requestDict["new_settings"] = jobDict;
         string requestJson = JsonSerializer.Serialize(requestDict);

         var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase}/{apiVersion}/jobs/reset");
         request.Content = new StringContent(requestJson);
         HttpResponseMessage response = await SendAsync(request);
         await EnsureSuccessOrThrow(response);
      }

      public async Task<IReadOnlyCollection<ClusterInfo>> LsClusters()
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBase20}/clusters/list");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         ClustersListReponse lst = JsonSerializer.Deserialize<ClustersListReponse>(rjson);

         return (lst == null || lst.Clusters == null || lst.Clusters.Length == 0)
            ? new ClusterInfo[0]
            : lst.Clusters.ToArray();

      }

      public async Task<ClusterInfo> LoadCluster(string clusterId)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBase20}/clusters/get?cluster_id={clusterId}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         return JsonSerializer.Deserialize<ClusterInfo>(rjson);
      }

      public async Task StartCluster(string clusterId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase20}/clusters/start");
         request.Content = new StringContent($"{{\"cluster_id\":\"{clusterId}\"}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
      }

      public async Task RestartCluster(string clusterId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase20}/clusters/restart");
         request.Content = new StringContent($"{{\"cluster_id\":\"{clusterId}\"}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();

      }

      public async Task TerminateCluster(string clusterId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase20}/clusters/delete");
         request.Content = new StringContent($"{{\"cluster_id\":\"{clusterId}\"}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
      }

      public async Task<IReadOnlyCollection<ClusterEvent>> LsClusterEvents(string clusterId)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase20}/clusters/events");
         request.Content = new StringContent($"{{\"cluster_id\":\"{clusterId}\"}}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         EventsListReponse evts = JsonSerializer.Deserialize<EventsListReponse>(rjson);

         return evts.Events;
      }

      public async Task<IReadOnlyCollection<ObjectInfo>> LsWorkspace(IOPath path)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBase20}/workspace/list");
         request.Content = new StringContent(JsonSerializer.Serialize(new WorkspaceLsRequest { Path = path }));
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         WorkspaceLsResponse objs = JsonSerializer.Deserialize<WorkspaceLsResponse>(rjson);
         return objs.Objects;
      }

      public async Task<IReadOnlyCollection<SqlQuery>> LsSqlQueries(Func<long, long, Task> progress = null)
      {
         const long pageSize = 25;
         long pageNo = 0;
         var result = new List<SqlQuery>();
         long totalCount;
         do
         {
            (IReadOnlyCollection<SqlQuery> queries, long totalCount1) = await ListSqlQueries(pageNo++, pageSize);
            totalCount = totalCount1;
            result.AddRange(queries);

            if(progress != null)
               await progress(result.Count, totalCount);
         }
         while(result.Count < totalCount);

         if(progress != null)
            await progress(totalCount, totalCount);

         return result;
      }

      public async Task<IReadOnlyCollection<SqlDashboard>> LsSqlDashboards(Func<long, long, Task> progress = null)
      {
         long pageNo = 0;
         const long pageSize = 50;
         var result = new List<SqlDashboard>();

         while(true)
         {
            ListSqlDashboardsResponse r = await GetAsync<ListSqlDashboardsResponse>(
               $"{_sqlPreviewBase}/dashboards?page={++pageNo}&page_size={pageSize}");

            result.AddRange(r.Results);

            if(r.Results.Length < pageSize)
               break;
         }

         return result;
      }

      public async Task<string> GetSqlDashboardRaw(string dashboardId)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_sqlPreviewBase}/dashboards/{dashboardId}");
         HttpResponseMessage response = await SendAsync(request);
         if(response.StatusCode == HttpStatusCode.NotFound)
            return null;
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();
         return rjson;
      }

      public async Task<IReadOnlyCollection<DataSource>> LsDataSources()
      {
         return await GetAsync<DataSource[]>($"{_sqlPreviewBase}/data_sources");
      }


      public async Task<IReadOnlyCollection<SqlEndpoint>> LsSqlEndpoints()
      {
         // endpoints API: https://docs.microsoft.com/en-us/azure/databricks/sql/api/sql-endpoints

         var request = new HttpRequestMessage(HttpMethod.Get, $"{_sqlBase}/endpoints");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();
         ListEndpointsResponse r = JsonSerializer.Deserialize<ListEndpointsResponse>(rjson);
         return r?.Endpoints ?? Array.Empty<SqlEndpoint>();
      }

      private async Task<Tuple<IReadOnlyCollection<SqlQuery>, long>> ListSqlQueries(long pageNo, long pageSize)
      {
         // https://redocly.github.io/redoc/?url=https://docs.microsoft.com/azure/databricks/_static/api-refs/queries-dashboards-2.0-azure.yaml#operation/sql-analytics-get-queries

         // pages are 1 - based, not 0 like normal people!
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_sqlPreviewBase}/queries?page={pageNo + 1}&page_size={pageSize}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         ListSqlQueriesResponse r = JsonSerializer.Deserialize<ListSqlQueriesResponse>(rjson);

         return new Tuple<IReadOnlyCollection<SqlQuery>, long>(r.Results, r.Count);
      }

      public async Task<string> GetSqlQueryRaw(string queryId)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_sqlPreviewBase}/queries/{queryId}");
         HttpResponseMessage response = await SendAsync(request);
         if(response.StatusCode == HttpStatusCode.NotFound)
            return null;
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();
         return rjson;
      }

      public async Task<SqlQuery> GetSqlQuery(string queryId)
      {
         string raw = await GetSqlQueryRaw(queryId);

         if(raw == null)
            return null;

         SqlQuery r = JsonSerializer.Deserialize<SqlQuery>(raw);

         return r;
      }

      public async Task UpdateSqlQueryRaw(string queryId, string rawJson)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_sqlPreviewBase}/queries/{queryId}");
         request.Content = new StringContent(rawJson);
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
      }

      public async Task<string> CreateSqlQueryRaw(string rawJson)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_sqlPreviewBase}/queries");
         request.Content = new StringContent(rawJson);
         HttpResponseMessage response = await SendAsync(request);
         await EnsureSuccessOrThrow(response);

         string rjson = await response.Content.ReadAsStringAsync();
         SqlQuery result = JsonSerializer.Deserialize<SqlQuery>(rjson);
         return result.Id;
      }

      public async Task DeleteSqlQuery(string queryId)
      {
         var request = new HttpRequestMessage(HttpMethod.Delete, $"{_sqlPreviewBase}/queries/{queryId}");
         HttpResponseMessage response = await SendAsync(request);
         await EnsureSuccessOrThrow(response);
      }

      private static string ToString(SqlObjectType t) => t switch
      {
         // https://redocly.github.io/redoc/?url=https://docs.microsoft.com/azure/databricks/_static/api-refs/queries-dashboards-2.0-azure.yaml#operation/get-sql-analytics-object-permissions

         SqlObjectType.Query => "queries",
         SqlObjectType.Dashboard => "dashboards",
         SqlObjectType.Alert => "alerts",
         SqlObjectType.DataSource => "data_sources",
         _ => throw new NotImplementedException()
      };

      public async Task<IReadOnlyCollection<AclEntry>> GetAcl(SqlObjectType objectType, string queryId)
      {
         // see https://redocly.github.io/redoc/?url=https://docs.microsoft.com/azure/databricks/_static/api-refs/queries-dashboards-2.0-azure.yaml#operation/get-sql-analytics-object-permissions
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_sqlPreviewBase}/permissions/{ToString(objectType)}/{queryId}");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();

         GetAclResponse r = JsonSerializer.Deserialize<GetAclResponse>(rjson);

         return r.Acl;
      }

      public async Task SetAcl(SqlObjectType objectType, string objectId, IEnumerable<AclEntry> acl)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, $"{_sqlPreviewBase}/permissions/{ToString(objectType)}/{objectId}");
         string jacl = JsonSerializer.Serialize(acl);
         request.Content = new StringContent(jacl);
         HttpResponseMessage response = await SendAsync(request);
         await EnsureSuccessOrThrow(response);
      }

      public async Task<ScimUser> ScimWhoami()
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_scimBase}/Me");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();
         return JsonSerializer.Deserialize<ScimUser>(rjson);
      }

      public async Task LsScimSp()
      {
         var request = new HttpRequestMessage(HttpMethod.Get, $"{_scimBase}/ServicePrincipals");
         HttpResponseMessage response = await SendAsync(request);
         response.EnsureSuccessStatusCode();
         string rjson = await response.Content.ReadAsStringAsync();
      }

      public async Task<IReadOnlyCollection<ScimUser>> LsScimUsers()
      {
         // single request actually lists all the users
         GetScimUsersResponse response = await GetAsync<GetScimUsersResponse>($"{_scimBase}/Users");

         return response.Resources;
      }

      public async Task<string> Exec(string clusterId, Language language, string command, Action<string> progressCallback)
      {
         string languageParam = language.ToString().ToLower();

         // create execution context
         ExecutionContextResponse context = await PostAsync<ExecutionContextResponse>(
            $"{_apiBase12}/contexts/create",
            new ExecutionContextRequest { ClusterId = clusterId, Language = languageParam });

         if(progressCallback != null)
            progressCallback("context created");

         // wait till it's available
         while(context.Status != "Running")
         {
            if(context.Status != null)
            {
               await Task.Delay(1000);
            }

            context = await GetAsync<ExecutionContextResponse>(
               $"{_apiBase12}/contexts/status?clusterId={clusterId}&contextId={context.Id}");

            if(progressCallback == null)
               progressCallback($"context status: {context.Status}, id: {context.Id}");
         }

         // execute command
         ExecutionContextResponse runningCommand = await PostAsync<ExecutionContextResponse>(
            $"{_apiBase12}/commands/execute",
            new ExecutionContextRequest
            {
               ClusterId = clusterId,
               ContextId = context.Id,
               Language = languageParam,
               Command = command
            });

         if(progressCallback != null)
            progressCallback($"command created, id: {runningCommand.Id}");

         // wait till it's finished
         while(runningCommand.Status != "Finished" && runningCommand.Status != "Error")
         {
            if(runningCommand.Status != null)
               await Task.Delay(1000);

            runningCommand = await GetAsync<ExecutionContextResponse>(
               $"{_apiBase12}/commands/status?clusterId={clusterId}&contextId={context.Id}&commandId={runningCommand.Id}");

            if(progressCallback != null)
               progressCallback($"command status: {runningCommand.Status}");
         }

         // destroy context
         await PostAsync<ExecutionContextResponse>(
            $"{_apiBase12}/contexts/destroy",
            new ExecutionContextRequest
            {
               ClusterId = clusterId,
               ContextId = context.Id
            });

         return runningCommand.Results?.ToString();
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
         public SqlQuery[] Results { get; set; }
      }

      public class ListEndpointsResponse
      {
         [JsonPropertyName("endpoints")]
         public SqlEndpoint[] Endpoints { get; set; }
      }

      public class ListSqlDashboardsResponse
      {
         [JsonPropertyName("count")]
         public long Count { get; set; }

         [JsonPropertyName("results")]
         public SqlDashboard[] Results { get; set; }
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

      public class CreateJobResponse
      {
         [JsonPropertyName("job_id")]
         public long JobId { get; set; }
      }

      public class ExecutionContextRequest
      {
         [JsonPropertyName("clusterId")]
         public string ClusterId { get; set; }

         [JsonPropertyName("contextId")]
         public string ContextId { get; set; }

         [JsonPropertyName("language")]
         public string Language { get; set; }

         [JsonPropertyName("command")]
         public string Command { get; set; }

         /// <summary>
         /// An optional map of values used downstream. For example, a displayRowLimit override (used in testing).
         /// </summary>
         [JsonPropertyName("options")]
         public string CommandOptions { get; set; }
      }

      public class ExecutionContextResponse
      {
         [JsonPropertyName("id")]
         public string Id { get; set; }

         [JsonPropertyName("status")]
         public string Status { get; set; }

         /// <summary>
         /// Command execution results
         /// </summary>
         [JsonPropertyName("results")]
         public object Results { get; set; }
      }

      public class GetScimUsersResponse
      {
         [JsonPropertyName("totalResults")]
         public long TotalResults { get; set; }

         [JsonPropertyName("startIndex")]
         public long StartIndex { get; set; }

         [JsonPropertyName("itemsPerPage")]
         public long ItemsPerPage { get; set; }

         [JsonPropertyName("Resources")]
         public ScimUser[] Resources { get; set; }
      }

      #endregion
   }
}
