using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage {
    /// <summary>
    /// Storage with caching capabilities.
    /// </summary>
    public interface ICachedStorage : IFileStorage {
        /// <summary>
        /// Invalidate a path in the cache.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if path was successfully invalidated, false if the entry did not exist so there was nothing to do.</returns>
        Task<bool> Invaliadate(IOPath path, CancellationToken cancellationToken);

        /// <summary>
        /// Clears all cache entries
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task Clear(CancellationToken cancellationToken);
    }
}
