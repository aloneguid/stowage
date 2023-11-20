﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stowage {
    /// <summary>
    /// Slim interface providing access to a file storage.
    /// </summary>
    public interface IFileStorage : IDisposable {
        /// <summary>
        /// List entries on the path specified. An entry is usually a file, however can be a folder as well (or maybe something else in future). 
        /// Just check documentation on <see cref="IOEntry"/> to understand what it can be. Note that <see cref="IOEntry"/> has implicit conversions to/from a string, so in most of the cases you shouldn't even care.
        /// </summary>
        /// <param name="path">Path to list on. Passing <see langword="null"/> simply lists in the root of the filesystem. Remember that <see cref="IOEntry"/> has implicit to/from string conversions, so you don't have to construct it explicitly and just pass a string.</param>
        /// <param name="recurse"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>List of object in this path, including files and folders. Folders end with "/" and files don't.</returns>
        Task<IReadOnlyCollection<IOEntry>> Ls(
           IOPath? path = null,
           bool recurse = false,
           CancellationToken cancellationToken = default);


        /// <summary>
        /// Opens the blob stream to read.
        /// </summary>
        /// <param name="path">Blob's full path</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Stream in an open state, or null if blob doesn't exist by this ID. It is your responsibility to close and dispose this
        /// stream after use.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        /// <exception cref="ArgumentException">Thrown when ID is too long. Long IDs are the ones longer than 50 characters.</exception>
        Task<Stream?> OpenRead(IOPath path, CancellationToken cancellationToken = default);

        Task<string> ReadText(IOPath path, Encoding encoding = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads blob as utf-8 text, then uses <see cref="System.Text.Json"/> to deserialise as json object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> ReadAsJson<T>(IOPath path, CancellationToken cancellationToken = default);


        /// <summary>
        /// Open blob for writing as a stream. If the blob already exists it will be quietly overwritten.
        /// Unfortunately there is no cross-cloud way to append to blobs as most providers do not support them (the only reliable one is Azure Blob Storage).
        /// You can keep writing to the stream for as long as you want - the data will be dumpted periodically to online storage.
        /// The blob is considered finished when you dispose the stream.
        /// Although the returned stream is fully compatible with synchronous API, please prefer async write and async dispose when possible.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Stream> OpenWrite(IOPath path, CancellationToken cancellationToken = default);

        Task WriteText(IOPath path, string contents, Encoding encoding = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uses <see cref="System.Text.Json"/> to first serialize the object, and then write it as utf-8 text.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value">Object value to serialize.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WriteAsJson(IOPath path, object value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a single object by it's full path.
        /// </summary>
        /// <param name="path">Path to delete. If this path points to a folder, the folder is deleted recursively.</param>
        /// <param name="recurse">Considers path parameters a directory (or a container, or a path prefix, depending on implementation) and removes everything recursively.</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException">Thrown when ID is null.</exception>
        /// <exception cref="ArgumentException">Thrown when ID is too long. Long IDs are the ones longer than 50 characters.</exception>
        Task Rm(IOPath path, bool recurse = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if blobs exists in the storage
        /// </summary>
        /// <param name="path">Path to check for.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>List of results of true and false indicating existence</returns>
        Task<bool> Exists(IOPath path, CancellationToken cancellationToken = default);

        Task Ren(IOPath name, IOPath newName, CancellationToken cancellationToken = default);

        // todo:
        // - read/write as bytes (maybe span?)
        // - read to stream
        // - read to file
        // - write from file
        // - json r/w
        // - copy
        // - rename
        // - move
    }
}