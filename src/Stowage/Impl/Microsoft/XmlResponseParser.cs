using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Stowage.Impl.Microsoft {

    static class XmlResponseParser {

        private static IEnumerable<IOEntry> ConvertBlobBatch(XElement blobs) {
            // https://learn.microsoft.com/en-us/rest/api/storageservices/list-blobs

            foreach(XElement blobPrefix in blobs.Elements("BlobPrefix")) {
                string? name = blobPrefix.Element("Name")?.Value;
                yield return new IOEntry(name + IOPath.PathSeparatorString);
            }

            foreach(XElement blob in blobs.Elements("Blob")) {
                string? name = blob.Element("Name")?.Value;
                if(name == null)
                    continue;
                var file = new IOEntry(name);

                XElement properties = blob.Element("Properties");
                if(properties != null) {
                    foreach(XElement xp in properties.Elements()) {
                        string pname = xp.Name.ToString();
                        string pvalue = xp.Value;

                        if(!string.IsNullOrEmpty(pvalue)) {
                            if(pname == "Last-Modified") {
                                file.LastModificationTime = DateTimeOffset.Parse(pvalue);
                            } else if(pname == "Content-Length") {
                                file.Size = long.Parse(pvalue);
                            } else if(pname == "Content-MD5") {
                                file.MD5 = pvalue;
                            } else {
                                file.Properties[pname] = pvalue;
                            }
                        }
                    }
                }
                yield return file;
            }
        }

        public static void ParseBlobListResponse(string rawXml, List<IOEntry> result, out string? nextMarker) {
            XElement x = XElement.Parse(rawXml);
            XElement? blobs = x.Element("Blobs");
            if(blobs != null) {
                result.AddRange(ConvertBlobBatch(blobs));
            }
            nextMarker = x.Element("NextMarker")?.Value;
            if(string.IsNullOrEmpty(nextMarker))
                nextMarker = null;
        }
    }
}