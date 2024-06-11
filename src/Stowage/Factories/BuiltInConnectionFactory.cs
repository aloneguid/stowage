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

                // using CLI profile
                string? cliProfile = connectionString.Get(KnownParameter.AwsProfile);
                if(!string.IsNullOrEmpty(cliProfile)) {
                    string? region = connectionString.Get(KnownParameter.Region);
                    return Files.Of.AmazonS3FromCliProfile(cliProfile, region);
                }

                // using long-term credentials
                string? accessKey = connectionString.Get(KnownParameter.KeyId);
                if(!string.IsNullOrEmpty(accessKey)) {
                    connectionString.GetRequired(KnownParameter.KeyOrPassword, true, out string secretKey);
                    string? region = connectionString.Get(KnownParameter.Region);
                    return Files.Of.AmazonS3(accessKey, secretKey, region);
                }

                return null;
            }

            return null;
        }
    }
}