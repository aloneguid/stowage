using System;
using System.Collections.Generic;
using System.Text;
using Stowage.Impl;
using Stowage.Impl.Databricks;
using Stowage.Impl.Microsoft;

namespace Stowage.Factories {
    class BuiltInConnectionFactory : IConnectionFactory {
        public IFileStorage? Create(ConnectionString connectionString) {
            if(connectionString.Prefix == "disk") {
                string? path = connectionString.Get("path");

                return path == null ? Files.Of.EntireLocalDisk(): Files.Of.LocalDisk(path);
            }

            if(connectionString.Prefix == "inmemory") {
                return InMemoryFileStorage.CreateOrGet(null);
            }

            if(connectionString.Prefix == "az") {
                connectionString.GetRequired(KnownParameter.AccountName, true, out string accountName);

                string? sharedKey = connectionString.Get(KnownParameter.KeyOrPassword);
                if(!string.IsNullOrEmpty(sharedKey)) {
                    return new AzureBlobFileStorage(accountName, new SharedKeyAuthHandler(accountName, sharedKey));
                }
            }

            if(connectionString.Prefix == "dbfs") {
                connectionString.GetRequired(KnownParameter.Url, true, out string url);
                connectionString.GetRequired(KnownParameter.Token, true, out string token);

                return new DatabricksRestClient(new Uri(url), token);
            }

            if(connectionString.Prefix == "s3") {

                // using long-term credentials
                string? accessKey = connectionString.Get(KnownParameter.KeyId);
                if(!string.IsNullOrEmpty(accessKey)) {
                    connectionString.GetRequired(KnownParameter.KeyOrPassword, true, out string secretKey);
                    connectionString.GetRequired(KnownParameter.Region, true, out string region);
                    string? sessionToken = connectionString.Get(KnownParameter.SessionToken);
                    return Files.Of.AmazonS3(accessKey, secretKey, region, sessionToken);
                }

                // using CLI profile
                return Files.Of.AmazonS3FromCliProfile(
                    connectionString.Get(KnownParameter.AwsProfile),
                    connectionString.Get(KnownParameter.Region));
            }

            return null;
        }
    }
}