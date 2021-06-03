using System;
using System.Collections.Generic;
using System.Text;
using Stowage.Impl;
using Stowage.Impl.Databricks;
using Stowage.Impl.Microsoft;

namespace Stowage.Factories
{
   class BuiltInConnectionFactory : IConnectionFactory
   {
      public IFileStorage Create(ConnectionString connectionString)
      {
         if(connectionString.Prefix == "disk")
         {
            connectionString.GetRequired("path", true, out string path);

            return new LocalDiskFileStorage(path);
         }

         if(connectionString.Prefix == "inmemory")
         {
            return InMemoryFileStorage.CreateOrGet(null);
         }

         if(connectionString.Prefix == "az")
         {
            connectionString.GetRequired(KnownParameter.AccountName, true, out string accountName);
            connectionString.GetRequired(KnownParameter.BucketName, true, out string containerName);

            string sharedKey = connectionString.Get(KnownParameter.KeyOrPassword);
            if(!string.IsNullOrEmpty(sharedKey))
            {
               return new AzureBlobFileStorage(accountName, containerName, new SharedKeyAuthHandler(accountName, sharedKey));
            }
         }

         if(connectionString.Prefix == "dbfs")
         {
            connectionString.GetRequired(KnownParameter.Url, true, out string url);
            connectionString.GetRequired(KnownParameter.Token, true, out string token);

            return new DatabricksDbfsStorage(new Uri(url), token);
         }

         return null;
      }
   }
}
