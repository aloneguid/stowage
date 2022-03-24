using System;
using System.Runtime.CompilerServices;
using Stowage.Impl;
using Stowage.Impl.Amazon;
using Stowage.Impl.Databricks;
using Stowage.Impl.Google;
using Stowage.Impl.Microsoft;

[assembly: InternalsVisibleTo("Stowage.Test, PublicKey=00240000048000009400000006020000002400005253413100040000010001001586d122f32211df78c3f502f97879e8ded0d3a9a2b1eb36cc9606730cf0905ab3a15b8045bd5691784302ab0c818b59b839ecb186ac92e4892469e648b43ffe45a2c68681a56bddd0002f0543713214c37451d5309930b911f1c910731da6297b7f1a607a49f43f99c790efe81308267d7c8d3cc3f10fcd3efaa64f23409cac")]

namespace Stowage
{
   public interface IFilesFactory
   {
   }

   public static class Files
   {
      private static Action<string> _logMessage;

      private static readonly IFilesFactory _filesFactory = new EmptyFilesFactory();

      public static IFilesFactory Of => _filesFactory;

      sealed class EmptyFilesFactory : IFilesFactory
      {

      }

      public static IDisposable The(this IFilesFactory _, IFileStorage storage)
      {
         if(storage is null)
            throw new ArgumentNullException(nameof(storage));

         return IOContext.Push(storage);
      }

      public static IFileStorage ConnectionString(this IFilesFactory _, string connnectionString)
      {
         return ConnectionStringFactory.Create(connnectionString);

         //return IOContext.Push(ConnectionStringFactory.Create(connnectionString));
      }

      public static IFileStorage LocalDisk(this IFilesFactory _, string rootDir)
      {
         return new LocalDiskFileStorage(rootDir);
      }

      public static IFileStorage InternalMemory(this IFilesFactory _, string id = null)
      {
         return InMemoryFileStorage.CreateOrGet(id);
      }

      public static IVirtualStorage VFS(this IFilesFactory _)
      {
         return new VirtualStorage();
      }

      public static IFileStorage AzureBlobStorage(this IFilesFactory _, string accountName, string sharedKey, string containerName = null)
      {
         return new AzureBlobFileStorage(accountName, containerName, new SharedKeyAuthHandler(accountName, sharedKey));
      }

      /*public static IFileStorage AzureTableStorage(
         this IFilesFactory _, string accountName, string sharedKey)
      {
         return new AzureTableFileStorage(accountName,
            new  SharedKeyAuthHandler(accountName, sharedKey, true));
      }*/

      public static IFileStorage AmazonS3(this IFilesFactory _, string bucketName, string accessKeyId, string secretAccessKey, string region)
      {
         return AmazonS3(_, bucketName, accessKeyId, secretAccessKey, region, new Uri($"https://{bucketName}.s3.amazonaws.com"));
      }

      public static IFileStorage AmazonS3(this IFilesFactory _, string bucketName, string accessKeyId, string secretAccessKey, string region, Uri endpoint)
      {
         return new AwsS3FileStorage(
            endpoint,
            new S3AuthHandler(accessKeyId, secretAccessKey, region));
      }

      public static IFileStorage DigitalOceanSpaces(this IFilesFactory _, string region, string accessKeyId, string secretAccessKey)
      {
         return new AwsS3FileStorage(
            new Uri($"https://{region}.digitaloceanspaces.com"),
            new S3AuthHandler(accessKeyId, secretAccessKey, region));
      }


      public static IFileStorage GoogleCloudStorage(this IFilesFactory _, string bucketName,
         string jsonCredential)
      {
         return new GoogleCloudStorage(bucketName, new GoogleAuthHandler(GoogleCredential.FromJson(jsonCredential)));
      }

      public static IFileStorage DatabricksDbfs(this IFilesFactory _, Uri workspaceUri, string token)
      {
         return new DatabricksRestClient(workspaceUri, token);
      }

      /// <summary>
      /// Constructs instance from the local profile. First looks at environment variables DATABRICKS_HOST and DATABRICKS_TOKEN.
      /// If found, takes them. Otherwise tries to look at local profile (~/.databrickscfg). Fails when nothing found.
      /// </summary>
      /// <param name="_"></param>
      /// <param name="profileName"></param>
      /// <returns></returns>
      public static IFileStorage DatabricksDbfsFromLocalProfile(this IFilesFactory _, string profileName = "DEFAULT")
      {
         return new DatabricksRestClient(profileName);
      }

      // todo: log callback
      public static void SetLogger(Action<string> logMessage)
      {
         _logMessage = logMessage;

         if(_logMessage != null)
         {
            _logMessage("logger set");
         }
      }

      public static bool HasLogger => _logMessage != null;

      internal static void Log(string message)
      {
         if(!HasLogger)
            return;

         _logMessage(message);
      }

   }
}
