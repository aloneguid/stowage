using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stowage {
    /// <summary>
    /// Storage Path representation wrapper. Makes sure path is safe and minimises amount of transformations required in order to keep it safe.
    /// </summary>
    public sealed class IOPath : IEquatable<IOPath> {
        /// <summary>
        /// Character used to split paths 
        /// </summary>
        public const char PathSeparator = '/';

        /// <summary>
        /// Character used to split paths as a string value
        /// </summary>
        public static readonly string PathSeparatorString = new string(PathSeparator, 1);

        /// <summary>
        /// Character used to split paths, as array of single character.
        /// </summary>
        public static readonly char[] PathSeparatorChar = new[] { PathSeparator };

        /// <summary>
        /// Returns '/'
        /// </summary>
        public static readonly string RootFolderPath = "/";

        /// <summary>
        /// Folder name for leveling up the path
        /// </summary>
        public static readonly string LevelUpFolderName = "..";


        private readonly string _path;
        private readonly string _name;
        private readonly string _folderPath;
        private string _parent;
        private string _pathWithTrailingSlash;
        private string _pathNoLeadingSlash;
        private string _pathWtithNoLeadingAndWithTralingSlash;

        /// <summary>
        /// Shorthand for root path
        /// </summary>
        public static IOPath Root { get; } = new IOPath(RootFolderPath);

        public IOPath(string path) {
            _path = Normalize(path);

            if(IsRoot(path)) {
                _name = RootFolderPath;
                _folderPath = RootFolderPath;
            } else {
                string[] parts = Split(path);

                _name = parts.Last();
                _folderPath = GetParent(path);
            }
        }

        public IOPath(params string?[] parts) : this(IOPath.Combine(parts)) {

        }

        /// <summary>
        /// Gets full path, including trailing slash in case it's a folder.
        /// </summary>
        public string Full => _path;

        /// <summary>
        /// Full path with trailing slash.
        /// </summary>
        public string WTS => _pathWithTrailingSlash ??= Normalize(_path, appendTrailingSlash: true);

        /// <summary>
        /// Full path with no leading slash.
        /// </summary>
        public string NLS => _pathNoLeadingSlash ??= Normalize(_path, true);

        /// <summary>
        /// Full path with no leading and with trailing slash
        /// </summary>
        public string NLWTS => _pathWtithNoLeadingAndWithTralingSlash ??= Normalize(_path, true, true);

        /// <summary>
        /// Gets parent path of this item.
        /// </summary>
        public IOPath Parent => _parent ??= GetParent(_path);

        /// <summary>
        /// File or folder name. If path is root, returns "/". Folder names do not end with "/".
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Returns folder path. If path is root, returns "/". Ends with "/".
        /// </summary>
        public string Folder => _folderPath;

        /// <summary>
        /// Constructs a file blob by full ID
        /// </summary>
        public static implicit operator IOPath(string? path) {
            if(path == null)
                return IOPath.Root;

            return new IOPath(path);
        }

        /// <summary>
        /// Converts blob to string by using full path
        /// </summary>
        /// <param name="path"></param>
        public static implicit operator string(IOPath? path) => path?.Full ?? RootFolderPath;

        /// <summary>
        /// Combines parts of path
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        public static string Combine(IEnumerable<string?> parts) {
            if(parts == null)
                return Normalize(null);

            string? last = null;
            var cp = new List<string>();

            foreach(string? part in parts) {
                if(part == null)
                    continue;


                last = part;
                cp.Add(NormalizePart(part));
            }

            bool isFolder = last != null && last.EndsWith(PathSeparatorString);

            string normal = Normalize(string.Join(PathSeparatorString, cp));

            if(IsRoot(normal))
                return normal;

            return isFolder ? normal + PathSeparatorString : normal;
        }

        /// <summary>
        /// Gets parent path of this item.
        /// </summary>
        public static string? GetParent(string? path) {
            if(path == null)
                return null;

            path = Normalize(path);

            string[]? parts = Split(path);
            if(parts == null || parts.Length == 0)
                return null;

            return parts.Length > 1
               ? Combine(parts.Take(parts.Length - 1)) + PathSeparator
               : PathSeparatorString;
        }

        /// <summary>
        /// Get relative path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static string RelativeTo(string path, string root) {
            string[]? pathParts = Split(path);
            string[]? rootParts = Split(root);

            if(pathParts == null || rootParts == null || rootParts.Length >= pathParts.Length)
                return RootFolderPath;

            // make sure that root parts are the same as path parts
            for(int i = 0; i < rootParts.Length; i++) {
                if(rootParts[i] != pathParts[i])
                    return RootFolderPath;
            }

            return Combine(pathParts.Skip(rootParts.Length));

        }

        /// <summary>
        /// Combines parts of path
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        public static string Combine(params string?[] parts) => Combine((IEnumerable<string?>)parts);

        /// <summary>
        /// Combines this part with another part and returns a new instance of <see cref="IOPath"/>
        /// </summary>
        /// <param name="nextPart"></param>
        /// <returns></returns>
        public IOPath Combine(string nextPart) => Combine(_path, nextPart);

        /// <summary>
        /// Normalizes path. Normalisation makes sure that:
        /// - When path is null or empty returns root path '/'
        /// - path separators are trimmed from both ends
        /// </summary>
        /// <param name="path"></param>
        /// <param name="removeLeadingSlash"></param>
        /// <param name="appendTrailingSlash"></param>
        public static string Normalize(string? path, bool removeLeadingSlash = false, bool appendTrailingSlash = false) {
            if(IsRoot(path))
                return RootFolderPath;

            string[]? parts = Split(path);
            if(parts == null)
                return RootFolderPath;

            var r = new List<string>(parts.Length);
            foreach(string part in parts) {
                if(part == LevelUpFolderName) {
                    if(r.Count > 0) {
                        r.RemoveAt(r.Count - 1);
                    }
                } else {
                    r.Add(part);
                }

            }
            path = string.Join(PathSeparatorString, r);

            string normal = removeLeadingSlash
               ? path
               : PathSeparatorString + path;

            if(appendTrailingSlash && !normal.EndsWith(PathSeparatorString)) {
                normal += PathSeparatorString;
            }

            if(IsRoot(normal))
                return normal;

            return normal;
        }

        /// <summary>
        /// Normalizes path part
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static string NormalizePart(string part) {
            if(part == null)
                throw new ArgumentNullException(nameof(part));

            return part.Trim(PathSeparator);
        }

        /// <summary>
        /// Splits path in parts. Leading and trailing path separators are totally ignored. Note that it returns
        /// null if input path is null. Parent folder signatures are returned as a part of split, they are not removed.
        /// If you want to get an absolute normalized path use <see cref="Normalize(string, bool, bool)"/>
        /// </summary>
        public static string[]? Split(string? path) {
            if(path == null)
                return null;

            bool isFolder = path.EndsWith(PathSeparatorString);

            string[] parts = path.Split(new[] { PathSeparator }, StringSplitOptions.RemoveEmptyEntries).Select(NormalizePart).ToArray();

            if(isFolder && parts.Length > 0) {
                parts[parts.Length - 1] = parts[parts.Length - 1] + PathSeparatorString;
            }

            return parts;
        }

        /// <summary>
        /// Checks if path is root folder path, which can be an empty string, null, or the actual root path.
        /// </summary>
        public static bool IsRoot(string? path) {
            return string.IsNullOrEmpty(path) || path == RootFolderPath;
        }

        /// <summary>
        /// Checks if path is root folder path, which can be an empty string, null, or the actual root path.
        /// </summary>
        public bool IsRootPath => IsRoot(_path);

        /// <summary>
        /// Gets the root folder name
        /// </summary>
        public static string? GetRoot(string? path) {
            string[]? parts = Split(path);
            return parts == null || parts.Length == 1 ? null : parts[0];
        }

        /// <summary>
        /// Removes root folder from path
        /// </summary>
        public static string RemoveRoot(string path) {
            string[]? parts = Split(path);
            if(parts == null || parts.Length == 1)
                return path;

            return Combine(parts.Skip(1));

        }

        public void ExtractPrefixAndRelativePath(out string prefix, out IOPath relativePath) {
            if(IsRootPath) {
                prefix = RootFolderPath;
                relativePath = this;
                return;
            }

            string[] parts = Split(_path)!;
            prefix = parts[0].Trim(PathSeparatorChar);
            relativePath = Combine(parts.Skip(1))!;
        }

        /// <summary>
        /// Compare that two path entries are equal. This takes into account path entries which are slightly different as strings but identical in physical location.
        /// </summary>
        public static bool Compare(string path1, string path2) {
            return Normalize(path1) == Normalize(path2);
        }

        /// <summary>
        /// Replace file name
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newFileName"></param>
        /// <returns></returns>
        public static string Rename(string path, string newFileName) {
            string[]? parts = Split(path);

            if(parts == null || parts.Length == 1)
                return newFileName;

            return Combine(Combine(parts.Take(parts.Length - 1)), newFileName);
        }

        /// <summary>
        /// Prefix this path with another path
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public IOPath? Prefix(IOPath? prefix) {
            if(prefix == null)
                return this;

            var parts = new List<string>(Split(prefix)!);
            parts.AddRange(Split(_path)!);
            string result = Combine(parts);
            if(IsFolder && !result.EndsWith(PathSeparatorString))
                result += PathSeparatorString;
            return result;
        }

        /// <summary>
        /// Detects if path is a folder path i.e. ends with "/"
        /// </summary>
        public bool IsFolder => _name.EndsWith(PathSeparatorString);

        /// <summary>
        /// Simply checks if kind of this item is not a folder.
        /// </summary>
        public bool IsFile => !IsFolder;

        /// <summary>
        /// Returns current local disk path if it was mapped to root.
        /// </summary>
        public static IOPath CurrenLocalDiskPathIfMappedToRoot {
            get {
                string cp = Environment.CurrentDirectory;
                string path = cp.Replace("C:\\", "").Replace("\\", "/") + "/";
                return new IOPath(path);
            }
        }

        /// <summary>
        /// Equality check
        /// </summary>
        /// <param name="other"></param>
        public bool Equals(IOPath? other) {

            if(ReferenceEquals(other, null))
                return false;

            return
               other._path == _path;
        }

        /// <summary>
        /// Equality check
        /// </summary>
        /// <param name="other"></param>
        public override bool Equals(object? other) {
            if(ReferenceEquals(other, null))
                return false;
            if(ReferenceEquals(other, this))
                return true;
            if(other.GetType() != typeof(IOPath))
                return false;

            return Equals((IOPath)other);
        }

        /// <summary>
        /// Hash code calculation
        /// </summary>
        public override int GetHashCode() => _path.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => _path;
    }
}