using System;
using Xunit;
using Stowage;
using System.Threading.Tasks;
using Stowage.SelfTest;
using SysIO = System.IO;
using Config.Net;
using System.IO;
using Xunit.Sdk;
using System.Collections.Generic;
using System.Reflection;

namespace Stowage.Test.Integration
{
   public class StorageTestDataAttribute : DataAttribute
   {
      private readonly string _pathPrefix;
      private readonly IFileStorage _storage;

      public StorageTestDataAttribute(string pathPrefix = null)
      {
         _pathPrefix = pathPrefix;
      }

      private IFileStorage CreateStorage(string name)
      {
         ITestSettings settings = new ConfigurationBuilder<ITestSettings>()
            .UseIniFile("c:\\tmp\\integration-tests.ini")
            .Build();

         IFileStorage storage;

         switch(name)
         {
            case "AzureBlob":
               storage = Files.Of.AzureBlobStorage(settings.AzureStorageAccount, settings.AzureStorageKey, settings.AzureContainerName);
               break;
            //case "AzureTable":
            //   storage = Files.Of.AzureTableStorage(settings.AzureStorageAccount, settings.AzureStorageKey);
            //   break;
            case "S3":
               storage = Files.Of.AmazonS3(settings.AwsBucket, settings.AwsKey, settings.AwsSecret, settings.AwsRegion);
               break;
            case "GCP":
               storage = Files.Of.GoogleCloudStorage(settings.GcpBucket, settings.GcpCred.Base64Decode());
               break;
            case "Disk":
               string dirPath = "c:\\tmp\\storage-io-files";

               if(SysIO.Directory.Exists(dirPath))
                  SysIO.Directory.Delete(dirPath, true);

               storage = Files.Of.LocalDisk(dirPath);
               break;
            case "Mem":
               storage = Files.Of.InternalMemory(Guid.NewGuid().ToString());
               break;
            case "DBFS":
               storage = Files.Of.DatabricksDbfs(settings.DatabricksBaseUri, settings.DatabricksToken);
               break;

            default:
               throw new ArgumentException($"what's '{name}'?");
         }

         return storage;
      }

      public override IEnumerable<object[]> GetData(MethodInfo testMethod)
      {
         return FilesSelfTest.GetXUnitTestData(CreateStorage(testMethod.Name), _pathPrefix);
      }
   }

   [Trait("Category", "Integration")]
   public class AzureIntegrationTest : BuiltInIntegrationsTest
   {
      [Theory]
      [StorageTestData]
      public Task AzureBlob(string n, Func<Task> testMethod)
      {
         return testMethod();
      }

      //[Theory]
      //[StorageTestData("/integration")]
      //public Task AzureTable(string n, Func<Task> testMethod)
      //{
      //   return testMethod();
      //}
   }

   public class MemIntegrationTest : BuiltInIntegrationsTest
   {
      [Theory]
      [StorageTestData]
      public Task Mem(string n, Func<Task> testMethod)
      {
         return testMethod();
      }
   }

   public class DiskIntegrationTest : BuiltInIntegrationsTest
   {
      [Theory]
      [StorageTestData]
      public Task Disk(string n, Func<Task> testMethod)
      {
         return testMethod();
      }
   }

   [Trait("Category", "Integration")]
   public class S3IntegrationTest : BuiltInIntegrationsTest
   {
      [Theory]
      [StorageTestData]
      public Task S3(string n, Func<Task> testMethod)
      {
         return testMethod();
      }
   }

   [Trait("Category", "Integration")]
   public class GCPIntegrationTest : BuiltInIntegrationsTest
   {
      [Theory]
      [StorageTestData]
      public Task GCP(string n, Func<Task> testMethod)
      {
         return testMethod();
      }
   }

   [Trait("Category", "Integration")]
   public class DBFSIntegrationTest : BuiltInIntegrationsTest
   {
      [Theory]
      [StorageTestData("/FileStore/itest")]
      public Task DBFS(string n, Func<Task> testMethod)
      {
         return testMethod();
      }
   }

   public abstract class BuiltInIntegrationsTest
   {
      protected readonly ITestSettings settings;

      public BuiltInIntegrationsTest()
      {
         settings = new ConfigurationBuilder<ITestSettings>()
            .UseIniFile("c:\\tmp\\integration-tests.ini")
            .Build();
      }
   }
}
