using System;
using System.Collections.Generic;
using System.Text;

namespace Stowage.Impl.Amazon {
    /// <summary>
    /// AWS S3 bucket, see https://docs.aws.amazon.com/AmazonS3/latest/API/API_Bucket.html
    /// </summary>
    class Bucket {
        /// <summary>
        /// The name of the bucket.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Date the bucket was created. This date can change when making changes to your bucket, such as editing its bucket policy.
        /// </summary>
        public DateTimeOffset? CreationDate { get; set; }

        public override string ToString() => Name;
    }
}
