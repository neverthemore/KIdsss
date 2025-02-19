// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    /// <summary>
    /// Represents a project-relative path to a potential asset (file or folder) in the asset database.
    /// </summary>
    internal sealed record AssetPath
    {
        private readonly string localPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPath"/> object.
        /// </summary>
        /// <param name="path">
        /// An absolute or project-relative  path to a potential asset (file or folder) in the asset database.
        ///  For example, 'c:/Unity Projects/My Project/Assets/StreamingAssets' would get converted into 'Assets/StreamingAssets'.
        /// <para>
        /// <example> 'c:/Unity Projects/My Project/Assets/StreamingAssets' </example>
        /// <example> 'Assets/StreamingAssets' </example>
        /// </para>
        /// </param>
        /// <exception cref="ArgumentNullException"> Thrown if a <see langword="null"/> path argument is provided. </exception>
        public AssetPath([DisallowNull] string path) => localPath = PathUtils.GetRelativePath(path);

        /// <returns> <see langword="true"/> if a file exists at this path; otherwise, <see langword="false"/>. </returns>
        public bool HasFile() => File.Exists(localPath);

        /// <returns> <see langword="true"/> if a directory exists at this path; otherwise, <see langword="false"/>. </returns>
        public bool HasFolder() => Directory.Exists(localPath);

        /// <summary>
        /// Gets a value indicating whether this path points to a location inside the project folder.
        /// <para>
        /// This can be used to determine if it's possible to use the <see cref="AssetDatabase"/> class with this path.
        /// </para>
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if this path  points to a location inside the project folder; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsProjectRelativePath() => PathUtils.IsProjectRelativePath(localPath);

        /// <summary>
        /// Gets the absolute (full) representation of this path.
        /// </summary>
        public string ToFullPath() => PathUtils.GetFullPath(localPath);

        public override string ToString() => "\"" + localPath + "\"";
        public static implicit operator string(AssetPath assetPath) => assetPath?.localPath;
        public static implicit operator AssetPath(string fullOrLocalPath) => fullOrLocalPath is null ? null : new AssetPath(fullOrLocalPath);
    }
}
