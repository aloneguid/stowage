using System;

namespace Stowage.Test {
    public interface ITestSettings {

        string AzureStorageAccount { get; }

        string AzureContainerName { get; }

        #region [ Azure Shared Access Key Auth ]

        string AzureStorageKey { get; }

        #endregion

        #region [ Azure Entra Id Auth ]

        string AzureTenantId { get; }

        string AzureClientId { get; }

        string AzureClientSecret { get; }

        #endregion

        string AwsBucket { get; }

        string AwsKey { get; }

        string AwsSecret { get; }

        string AwsRegion { get; }

        Uri MinioEndpoint { get; }

        string MinioKey { get; }

        string MinioSecret { get; }

        string MinioBucket { get; }

        string GcpBucket { get; }

        string GcpCred { get; }

        Uri DatabricksBaseUri { get; }

        string DatabricksToken { get; }
    }
}