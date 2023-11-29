using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Stowage.Impl.Amazon {
    class XmlResponseParser {
        /// <summary>
        /// Parses out XML response. See specs at https://docs.aws.amazon.com/AmazonS3/latest/API/API_ListObjectsV2.html
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="continuationToken"></param>
        /// <returns></returns>
        public IReadOnlyCollection<IOEntry> ParseListObjectV2Response(string xml, out string? continuationToken) {
            continuationToken = null;
            var result = new List<IOEntry>();
            using(var sr = new StringReader(xml)) {
                using(var xr = XmlReader.Create(sr)) {
                    string? en = null;

                    while(xr.Read()) {
                        if(xr.NodeType == XmlNodeType.Element) {
                            switch(xr.Name) {
                                case "Contents":
                                    // object format specification: https://docs.aws.amazon.com/AmazonS3/latest/API/API_Object.html
                                    string? key = null;
                                    string? lastMod = null;
                                    string? eTag = null;
                                    string? size = null;
                                    string? storageClass = null;
                                    // read all the elements in this
                                    while(xr.Read() && !(xr.NodeType == XmlNodeType.EndElement && xr.Name == "Contents")) {
                                        if(xr.NodeType == XmlNodeType.Element)
                                            en = xr.Name;
                                        else if(xr.NodeType == XmlNodeType.Text) {
                                            switch(en) {
                                                case "Key":
                                                    key = xr.Value;
                                                    break;
                                                case "LastModified":
                                                    lastMod = xr.Value;
                                                    break;
                                                case "ETag":
                                                    eTag = xr.Value;
                                                    break;
                                                case "Size":
                                                    size = xr.Value;
                                                    break;
                                                case "StorageClass":
                                                    storageClass = xr.Value;
                                                    break;
                                            }
                                        }
                                    }

                                    if(key != null && !key.EndsWith("/")) {
                                        var entry = new IOEntry(key) {
                                            LastModificationTime = lastMod == null ? null : DateTimeOffset.Parse(lastMod),
                                            Size = size == null ? null : long.Parse(size)
                                        };
                                        entry.TryAddProperties(
                                           "ETag", eTag,
                                           "StorageClass", storageClass);
                                        result.Add(entry);
                                    }

                                    break;
                                case "CommonPrefixes":
                                    while(xr.Read() && !(xr.NodeType == XmlNodeType.EndElement && xr.Name == "CommonPrefixes")) {
                                        // <Prefix>foldername/</Prefix>
                                        if(xr.NodeType == XmlNodeType.Element)
                                            en = xr.Name;
                                        else if(xr.NodeType == XmlNodeType.Text) {
                                            if(en == "Prefix") {
                                                result.Add(new IOEntry(xr.Value));
                                            }
                                        }
                                    }
                                    break;
                                case "NextContinuationToken":
                                    if(xr.Read() && xr.NodeType == XmlNodeType.Text) {
                                        continuationToken = xr.Value;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public string? ParseInitiateMultipartUploadResponse(string xml) {
            using(var sr = new StringReader(xml)) {
                using(var xr = XmlReader.Create(sr)) {
                    while(xr.Read()) {
                        if(xr.NodeType == XmlNodeType.Element && xr.Name == "UploadId") {
                            xr.Read();
                            return xr.Value;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parses out XML response. See specs at https://docs.aws.amazon.com/AmazonS3/latest/API/API_ListBuckets.html
        /// </summary>
        /// <param name="xml"></param>
        public List<Bucket> ParseListBucketsResponse(string xml) {
            var result = new List<Bucket>();
            using(var sr = new StringReader(xml)) {
                using(var xr = XmlReader.Create(sr)) {
                    string? en = null;
                    while(xr.Read()) {
                        if(xr.NodeType == XmlNodeType.Element) {
                            switch(xr.Name) {
                                case "Bucket":
                                    string? name = null;
                                    DateTimeOffset? creationDate = null;

                                    while(xr.Read() && !(xr.NodeType == XmlNodeType.EndElement && xr.Name == "Bucket")) {
                                        if(xr.NodeType == XmlNodeType.Element)
                                            en = xr.Name;
                                        else if(xr.NodeType == XmlNodeType.Text) {
                                            switch(en) {
                                                case "Name":
                                                    name = xr.Value;
                                                    break;
                                                case "CreationDate":
                                                    creationDate = DateTimeOffset.Parse(xr.Value);
                                                    break;
                                            }
                                        }
                                    }

                                    if(name != null) {
                                        result.Add(new Bucket {
                                            Name = name,
                                            CreationDate = creationDate
                                        });
                                    }

                                    break;
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}