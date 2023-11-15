using System;

namespace Stowage.Test {
    public interface ITestSettings {
        string AzureStorageAccount { get; }

        string AzureStorageKey { get; }

        string AzureContainerName { get; }

        string AwsBucket { get; }

        string AwsKey { get; }

        string AwsSecret { get; }

        string AwsRegion { get; }

        Uri MinioEndpoint { get; }

        string MinioKey { get; }

        string MinioSecret { get; }

        string GcpBucket { get; }

        string GcpCred { get; }

        Uri DatabricksBaseUri { get; }

        string DatabricksToken { get; }
    }
}