﻿namespace Stowage {
    /// <summary>
    /// Known parameter names enouraged to be used in connection strings
    /// </summary>
    public static class KnownParameter {
        /// <summary>
        /// Indicates that this connection string is native
        /// </summary>
        public static readonly string Native = "native";

        /// <summary>
        /// Account or storage name
        /// </summary>
        public static readonly string AccountName = "account";

        /// <summary>
        /// Key or password
        /// </summary>
        public static readonly string KeyOrPassword = "key";

        /// <summary>
        /// Key ID
        /// </summary>
        public static readonly string KeyId = "keyId";

        /// <summary>
        /// Session token
        /// </summary>
        public static readonly string SessionToken = "st";

        /// <summary>
        /// Generic token
        /// </summary>
        public static readonly string Token = "token";

        /// <summary>
        /// Name of a local profile
        /// </summary>
        public static readonly string LocalProfileName = "profile";

        /// <summary>
        /// Bucket name
        /// </summary>
        public static readonly string BucketName = "bucket";

        /// <summary>
        /// Region
        /// </summary>
        public static readonly string Region = "region";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string UseDevelopmentStorage = "development";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string VaultUri = "vaultUri";

        /// <summary>
        /// Generic URL
        /// </summary>
        public static readonly string Url = "url";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string TenantId = "tenantId";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ClientId = "principalId";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ClientSecret = "principalSecret";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string MsiEnabled = "msi";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string IsLocalEmulator = "emu";

        /// <summary>
        /// AWS profile name (from ~/.aws/credentials)
        /// </summary>
        public static readonly string AwsProfile = "profile";
    }
}