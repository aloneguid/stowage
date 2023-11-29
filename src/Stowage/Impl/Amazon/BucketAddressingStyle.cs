using System;
using System.Collections.Generic;
using System.Text;

namespace Stowage.Impl.Amazon {
    /// <summary>
    /// S3 bucket addressing style, see https://docs.aws.amazon.com/AmazonS3/latest/dev/VirtualHosting.html
    /// </summary>
    public enum BucketAddressingStyle {
        /// <summary>
        /// Classic S3 addressing schema
        /// </summary>
        VirtualHost,

        /// <summary>
        /// The path-style scheme includes the name of the bucket in the URL path. For example, s3.amazonaws.com/bucket.
        /// </summary>
        Path
    }
}
