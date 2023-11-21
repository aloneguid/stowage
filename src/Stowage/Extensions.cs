using System;
using System.Collections.Generic;
using System.Text;

namespace Stowage {
    static class Extensions {
        public static string GetAbsolutePathUnencoded(this Uri uri) {
            string result = uri.ToString();

            // remove protocol
            int i = result.IndexOf("://");
            if(i >= 0)
                result = result.Substring(i + 3);

            // remove host and port
            i = result.IndexOf('/');
            if(i >= 0)
                result = result.Substring(i);

            // remove query string
            i = result.IndexOf('?');
            if(i >= 0)
                result = result.Substring(0, i);

            return result;
        }
    }
}
