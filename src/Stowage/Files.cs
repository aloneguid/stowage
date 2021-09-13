using System;
using System.Runtime.CompilerServices;
using Stowage.Impl;
using Stowage.Impl.Amazon;
using Stowage.Impl.Databricks;
using Stowage.Impl.Google;
using Stowage.Impl.Microsoft;

[assembly: InternalsVisibleTo("Stowage.Test")]

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

      public static IFileStorage AmazonS3(this IFilesFactory _, string bucketName, string accessKeyId, string secretAccessKey, string region)
      {
         return new AwsS3FileStorage(
            new Uri($"https://{bucketName}.s3.amazonaws.com"),
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
