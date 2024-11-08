using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using Stowage.Impl;
using Stowage.Impl.Amazon;
using Stowage.Impl.Databricks;
using Stowage.Impl.Google;
using Stowage.Impl.Microsoft;

[assembly: InternalsVisibleTo("Stowage.Test, PublicKey=00240000048000009400000006020000002400005253413100040000010001001586d122f32211df78c3f502f97879e8ded0d3a9a2b1eb36cc9606730cf0905ab3a15b8045bd5691784302ab0c818b59b839ecb186ac92e4892469e648b43ffe45a2c68681a56bddd0002f0543713214c37451d5309930b911f1c910731da6297b7f1a607a49f43f99c790efe81308267d7c8d3cc3f10fcd3efaa64f23409cac")]

namespace Stowage {
    /// <summary>
    /// Factory entry point
    /// </summary>
    public interface IFilesFactory {
    }

    /// <summary>
    /// Files factory
    /// </summary>
    public static class Files {
        private static Action<string>? _logMessage;

        private static readonly IFilesFactory _filesFactory = new EmptyFilesFactory();

        /// <summary>
        /// Create an instance of...
        /// </summary>
        public static IFilesFactory Of => _filesFactory;

        sealed class EmptyFilesFactory : IFilesFactory {

        }

        /// <summary>
        /// Context switcher for the current thread
        /// </summary>
        /// <param name="_"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IDisposable The(this IFilesFactory _, IFileStorage storage) {
            if(storage is null)
                throw new ArgumentNullException(nameof(storage));

            return IOContext.Push(storage);
        }

        /// <summary>
        /// Instantiate a storage from the connection string
        /// </summary>
        /// <param name="_"></param>
        /// <param name="connnectionString"></param>
        /// <returns></returns>
        public static IFileStorage ConnectionString(this IFilesFactory _, string connnectionString) {
            return ConnectionStringFactory.Create(connnectionString);
        }

        /// <summary>
        /// Instantiate a storage from the list of key-value pairs
        /// </summary>
        /// <param name="_"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static IFileStorage Parameters(this IFilesFactory _, Dictionary<string, string> parameters) {
            return ConnectionStringFactory.Create(parameters);
        }

        /// <summary>
        /// Instantiate local disk storage mapped to a directory.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="rootDir"></param>
        /// <returns></returns>
        public static IFileStorage LocalDisk(this IFilesFactory _, string rootDir) {
            return new LocalDiskFileStorage(rootDir);
        }

        /// <summary>
        /// Instantiate local disk storage mapped to entire local filesystem. On Windows, this will be mapped to
        /// the root of the current drive. On Linux, this will be mapped to the root filesystem.
        /// </summary>
        /// <param name="_"></param>
        /// <returns></returns>
        public static IFileStorage EntireLocalDisk(this IFilesFactory _) {
            string cp = Environment.CurrentDirectory;
            string root = new DirectoryInfo(cp).Root.FullName;

            return new LocalDiskFileStorage(root);
        }

        /// <summary>
        /// Instantiate internal memory storage
        /// </summary>
        /// <param name="_"></param>
        /// <param name="id">Optional instance identifier. Instances with the same ID are referring to the same instance.</param>
        /// <returns></returns>
        public static IFileStorage InternalMemory(this IFilesFactory _, string? id = null) {
            return InMemoryFileStorage.CreateOrGet(id);
        }

        /// <summary>
        /// Instantiate virtual filesystem storage
        /// </summary>
        /// <param name="_"></param>
        /// <returns></returns>
        public static IVirtualStorage VFS(this IFilesFactory _) {
            return new VirtualStorage();
        }

        /// <summary>
        /// Create Azure Blob Storage connection with Shared Key authentication
        /// </summary>
        /// <param name="_"></param>
        /// <param name="accountName"></param>
        /// <param name="sharedKey"></param>
        /// <returns></returns>
        public static IFileStorage AzureBlobStorage(this IFilesFactory _, string accountName, string sharedKey) {
            return new AzureBlobFileStorage(accountName, new SharedKeyAuthHandler(accountName, sharedKey));
        }

        /// <summary>
        /// Create Azure Blob Storage connection with Shared Key authentication and custom endpoint.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="endpoint"></param>
        /// <param name="accountName"></param>
        /// <param name="sharedKey"></param>
        /// <returns></returns>
        public static IFileStorage AzureBlobStorage(this IFilesFactory _, Uri endpoint, string accountName, string sharedKey) {
            return new AzureBlobFileStorage(endpoint, new SharedKeyAuthHandler(accountName, sharedKey));
        }

        public static IFileStorage AzureBlobStorage(this IFilesFactory _, string accountName, ClientSecretCredential clientSecretCredential) {
            return new AzureBlobFileStorage(accountName, new EntraIdAuthHandler(clientSecretCredential));
        }

        /// <summary>
        /// Creates a connection to Azure Blob Storage local emulator.
        /// </summary>
        /// <param name="_"></param>
        /// <returns></returns>
        public static IFileStorage AzureBlobStorageWithLocalEmulator(this IFilesFactory _) {
            const string accountName = "devstoreaccount1";
            const string sharedKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
            var endpoint = new Uri("http://127.0.0.1:10000/devstoreaccount1");
            return new AzureBlobFileStorage(endpoint, new SharedKeyAuthHandler(accountName, sharedKey));
        }

        /// <summary>
        /// Create Azure Blob Storage connection with Sas Token authentication.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="accountName"></param>
        /// <param name="sasToken"></param>
        /// <returns></returns>
        public static IFileStorage AzureBlobStorageWithSasToken(this IFilesFactory _, string accountName, string sasToken) {
            return new AzureBlobFileStorage(accountName, new SasAuthHandler(sasToken));
        }

        /// <summary>
        /// Create Azure Blob Storage connection with Sas Token authentication and custom endpoint.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="endpoint"></param>
        /// <param name="sasToken"></param>
        /// <returns></returns>
        public static IFileStorage AzureBlobStorageWithSasToken(this IFilesFactory _, Uri endpoint, string sasToken) {
            return new AzureBlobFileStorage(endpoint, new SasAuthHandler(sasToken));
        }

        /// <summary>
        /// Creates Amazon S3 provider
        /// </summary>
        /// <param name="_"></param>
        /// <param name="accessKeyId"></param>
        /// <param name="secretAccessKey"></param>
        /// <param name="region"></param>
        /// <param name="sessionToken">Optional session token</param>
        /// <returns></returns>
        public static IAwsS3FileStorage AmazonS3(this IFilesFactory _,
            string accessKeyId, string secretAccessKey, string region, string? sessionToken = null) {
            return AmazonS3(_, accessKeyId, secretAccessKey, region, 
                new Uri($"https://s3.amazonaws.com"),
                sessionToken);
        }

        /// <summary>
        /// Creates Amazon S3 provider
        /// </summary>
        /// <param name="_"></param>
        /// <param name="accessKeyId">Access key id</param>
        /// <param name="secretAccessKey">Secret access key</param>
        /// <param name="region"></param>
        /// <param name="endpoint"></param>
        /// <param name="sessionToken">Optional session token</param>
        /// <param name="bucketAddressingStyle"></param>
        /// <returns></returns>
        public static IAwsS3FileStorage AmazonS3(this IFilesFactory _,
            string accessKeyId, string secretAccessKey, string region, Uri endpoint,
            string? sessionToken = null,
            BucketAddressingStyle bucketAddressingStyle = BucketAddressingStyle.VirtualHost) {
            return new AwsS3FileStorage(
               endpoint,
               new S3AuthHandler(accessKeyId, secretAccessKey, sessionToken, region),
               bucketAddressingStyle);
        }

        /// <summary>
        /// Creates Amazon S3 provider using AWS CLI profile name. This also supports session tokens if they are present in the profile definition.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="profileName">Profile name. If not specified, will use default CLI profile.</param>
        /// <param name="region"></param>
        /// <returns></returns>
        public static IAwsS3FileStorage AmazonS3FromCliProfile(this IFilesFactory _,
            string? profileName = null,
            string? region = null) {
            
            var parser = new CredentialFileParser();
            parser.FillCredentials(profileName ?? CredentialFileParser.DefaultProfileName,
                out string accessKeyId, out string secretAccessKey,
                out string? sessionToken,
                out string? configRegion);

            if(region == null)
                region = configRegion;

            if(region == null)
                throw new InvalidOperationException("Region is required to be either passed explicitly or configured in AWS CLI config.");

            return AmazonS3(_, accessKeyId, secretAccessKey, region, new Uri("https://s3.amazonaws.com"), sessionToken);
        }

        /// <summary>
        /// Minio storage is a S3-compatible storage.
        /// Read more at https://min.io/docs/minio/linux/index.html
        /// </summary>
        /// <param name="_"></param>
        /// <param name="endpoint">Minio endpoint, for example http://localhost:9000. This endpoint should not include bucket name in hostname or path.</param>
        /// <param name="accessKeyId">Access key you can get from Minio's console "Access keys" tab.</param>
        /// <param name="secretAccessKey">Secret key for the access key above</param>
        /// <param name="region"></param>
        /// <returns></returns>
        public static IAwsS3FileStorage Minio(this IFilesFactory _,
            Uri endpoint,
            string accessKeyId,
            string secretAccessKey,
            string region = "") {

            return new AwsS3FileStorage(
                endpoint,
                new S3AuthHandler(accessKeyId, secretAccessKey, null, region),
                BucketAddressingStyle.Path);
        }

        public static IFileStorage DigitalOceanSpaces(this IFilesFactory _, string region, string accessKeyId, string secretAccessKey) {
            return new AwsS3FileStorage(
               new Uri($"https://{region}.digitaloceanspaces.com"),
               new S3AuthHandler(accessKeyId, secretAccessKey, null, region));
        }


        public static IFileStorage GoogleCloudStorage(this IFilesFactory _, string bucketName,
           string jsonCredential) {
            return new GoogleCloudStorage(bucketName, new GoogleAuthHandler(GoogleCredential.FromJson(jsonCredential)));
        }

        public static IFileStorage DatabricksDbfs(this IFilesFactory _, Uri workspaceUri, string token) {
            return new DatabricksRestClient(workspaceUri, token);
        }

        /// <summary>
        /// Constructs instance from the local profile. First looks at environment variables DATABRICKS_HOST and DATABRICKS_TOKEN.
        /// If found, takes them. Otherwise tries to look at local profile (~/.databrickscfg). Fails when nothing found.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="profileName"></param>
        /// <returns></returns>
        public static IFileStorage DatabricksDbfsFromLocalProfile(this IFilesFactory _, string profileName = "DEFAULT") {
            return new DatabricksRestClient(profileName);
        }

        /// <summary>
        /// Creates a wrapper around a storage that caches the content on local disk.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="parent">Storage interface to create caching around.</param>
        /// <param name="cacheDir">Caching directory. When not specified, will create 'stowage.cache' subdirectory in your OS'es temp directory.</param>
        /// <param name="maxAge">Maximum period to keep the files in.</param>
        /// <param name="clearOnDispose">Whether to scan storage and run checks and evictions</param>
        /// <returns></returns>
        public static ICachedStorage LocalDiskCacheStorage(this IFilesFactory _, IFileStorage parent,
            string? cacheDir = null,
            TimeSpan? maxAge = null,
            bool clearOnDispose = false) {

            string diskDir = cacheDir ?? Path.Combine(Path.GetTempPath(), "stowage.cache");

            var diskBackend = new LocalDiskFileStorage(diskDir);

            return new CachedFileContentStorage(parent,
                diskBackend,
                maxAge ?? TimeSpan.FromHours(1),
                clearOnDispose,
                true);
        }

        /// <summary>
        /// Creates a wrapper around a storage that caches the content in memory.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="parent">Storage interface to create caching around</param>
        /// <param name="memoryBlockId">Optional memory block id.</param>
        /// <param name="maxAge">Maximum period to keep the files in.</param>
        /// <param name="clearOnDispose">Whether to scan storage and run checks and evictions</param>
        /// <returns></returns>
        public static ICachedStorage MemoryCacheStorage(this IFilesFactory _, IFileStorage parent,
            string? memoryBlockId = null,
            TimeSpan? maxAge = null,
            bool clearOnDispose = false) {

            return new CachedFileContentStorage(parent,
                InMemoryFileStorage.CreateOrGet(memoryBlockId),
                maxAge ?? TimeSpan.FromHours(1),
                clearOnDispose,
                false);
        }

        /// <summary>
        /// Sets the logger for the library
        /// </summary>
        /// <param name="logMessage"></param>
        public static void SetLogger(Action<string> logMessage) {
            _logMessage = logMessage;

            if(_logMessage != null) {
                _logMessage("logger set");
            }
        }

        /// <summary>
        /// Returns true if the logger is set
        /// </summary>
        public static bool HasLogger => _logMessage != null;

        internal static void Log(string? message) {
            if(!HasLogger || message == null)
                return;

            _logMessage?.Invoke(message);
        }

    }
}