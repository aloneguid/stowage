using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stowage.Impl.Amazon;
using Xunit;

namespace Stowage.Test.Integration.Impl {

    [Trait("Category", "Integration")]
    public class AwsS3Test {
        private readonly IAwsS3FileStorage _storage;

        public AwsS3Test() {
            ITestSettings settings = ConfigLoader.Load();

            _storage = Files.Of.AmazonS3(settings.AwsKey, settings.AwsSecret, settings.AwsRegion,
                new Uri("https://s3.amazonaws.com"));
        }

        [Fact]
        public async Task ListBuckets() {
            IReadOnlyCollection<IOEntry> buckets = await _storage.Ls();
            Assert.NotEmpty(buckets);
        }

        [Fact]
        public async Task ListObjects() {
            IReadOnlyCollection<IOEntry> buckets = await _storage.Ls();
            IReadOnlyCollection<IOEntry> objects = await _storage.Ls(new IOPath(buckets.First().Name + "/"));
            Assert.NotEmpty(objects);
        }
    }
}
