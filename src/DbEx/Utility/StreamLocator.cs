// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DbEx.Utility
{
    /// <summary>
    /// <see cref="Stream"/> locator/manager.
    /// </summary>
    public static class StreamLocator
    {
        /// <summary>
        /// Gets the <b>Resource</b> content from the file system and then <c>Resources</c> folder within the <paramref name="assemblies"/> until found.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="assemblies">Assemblies to use to probe for assembly resource (in defined sequence); will check this assembly also (no need to specify).</param>
        /// <returns>The resource <see cref="StreamReader"/> where found; otherwise, <c>null</c>.</returns>
        internal static StreamReader? GetResourcesStreamReader(string fileName, params Assembly[]? assemblies) => GetStreamReader(fileName, "Resources", assemblies);

        /// <summary>
        /// Indicates whether the specified <b>Resource</b> content exists within the file system and then <c>Resources</c> folder within the <paramref name="assemblies"/> until found.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="assemblies">Assemblies to use to probe for assembly resource (in defined sequence); will check this assembly also (no need to specify).</param>
        /// <returns>The resource <see cref="Stream"/> where found; otherwise, <c>null</c>.</returns>
        internal static bool HasResourceStream(string fileName, params Assembly[]? assemblies) => HasStream(fileName, "Resources", assemblies);

        /// <summary>
        /// Gets the specified content from the file system and then <paramref name="contentType"/> folder within the <paramref name="assemblies"/> until found.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="contentType">The optional content type name.</param>
        /// <param name="assemblies">The assemblies to use to probe for the assembly resource (in defined sequence); will check this assembly also (no need to specify).</param>
        /// <returns>The resource <see cref="StreamReader"/> where found; otherwise, <c>null</c>.</returns>
        public static StreamReader? GetStreamReader(string fileName, string? contentType = null, params Assembly[]? assemblies)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            var fi = new FileInfo(fileName);
            if (fi.Exists)
                return new StreamReader(fi.FullName);

            if (!string.IsNullOrEmpty(contentType))
            {
                fi = new FileInfo(Path.Combine(fi.DirectoryName, contentType, fi.Name));
                if (fi.Exists)
                    return new StreamReader(fi.FullName);

                if (assemblies != null)
                {
                    var frn = ConvertFileNameToResourceName(fileName);
                    foreach (var ass in new List<Assembly>(assemblies) { typeof(StreamLocator).Assembly })
                    {
                        var rn = ass.GetManifestResourceNames().Where(x => x.EndsWith($".{contentType}.{frn}", StringComparison.InvariantCulture)).FirstOrDefault();
                        if (rn != null)
                        {
                            var ri = ass.GetManifestResourceInfo(rn);
                            if (ri != null)
                                return new StreamReader(ass.GetManifestResourceStream(rn)!);
                        }
                    }
                }
            }

            return null!;
        }

        /// <summary>
        /// Indicates whether the specified resource content exists within the file system or the <paramref name="contentType"/> folder within the <paramref name="assemblies"/> until found.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="contentType">The optional content type name.</param>
        /// <param name="assemblies">The assemblies to use to probe for the assembly resource (in defined sequence); will check this assembly also (no need to specify).</param>
        /// <returns><c>true</c> indicates that the <see cref="Stream"/> exists; otherwise, <c>false</c>.</returns>
        public static bool HasStream(string fileName, string? contentType, params Assembly[]? assemblies)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            var fi = new FileInfo(fileName);
            if (fi.Exists)
                return true;

            if (!string.IsNullOrEmpty(contentType))
            {
                fi = new FileInfo(Path.Combine(fi.DirectoryName, contentType, fileName));
                if (fi.Exists)
                    return true;

                if (assemblies != null)
                {
                    var frn = ConvertFileNameToResourceName(fileName);
                    foreach (var ass in new List<Assembly>(assemblies) { typeof(StreamLocator).Assembly })
                    {
                        var rn = ass.GetManifestResourceNames().Where(x => x.EndsWith($".{contentType}.{frn}", StringComparison.InvariantCulture)).FirstOrDefault();
                        if (rn != null)
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Replaces path characters to dot as per resource file notation.
        /// </summary>
        private static string ConvertFileNameToResourceName(string filename) => filename.Replace('/', '.').Replace('\\', '.');
    }
}