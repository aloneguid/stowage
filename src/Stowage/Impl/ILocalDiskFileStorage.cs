using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stowage.Impl {
    
    /// <summary>
    /// Local disk specific functionality.
    /// </summary>
    public interface ILocalDiskFileStorage : IFileStorage {

        /// <summary>
        /// Converts <see cref="IOPath"/> obtained from local disk storage to native local path, which will be different on different platforms.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string ToNativeLocalPath(IOPath path);
    }
}
