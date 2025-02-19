// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests
{
    using System;
    using System.IO;
    using Coherence.Tests;
    using NUnit.Framework;
    using UnityEngine;

    public class PathUtilsTests : CoherenceTest
    {
        private string PackagesAbsolutePath = Path.GetFullPath("Packages").Replace('\\', '/');
        private string LibraryAbsolutePath = Path.GetFullPath("Library").Replace('\\', '/');
        
        private string DataPathWithBackslashes => Application.dataPath.Replace('/', '\\');
        private string PackagesPathWithBackslashes => PackagesAbsolutePath.Replace('/', '\\');
        private string LibraryPathWithBackslashes => LibraryAbsolutePath.Replace('/', '\\');

        #region GetRelativePath - Assets Folder

        [Test]
        public void GetRelativePath_Should_ReturnInputAsIs_WhenGiven_NonProjectRelativePath()
        {
            const string input = "C:/My File.exe";
            const string expectedOutput = input;

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_Should_ReturnAnEmptyString_WhenGiven_AnEmptyString()
        {
            const string input = "";
            const string expectedOutput = "";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_Should_ThrowArgumentNullException_WhenGiven_ANullString()
        {
            const string input = null;

            var exception = Assert.Catch<ArgumentNullException>(() => _ = PathUtils.GetRelativePath(input));

            Assert.IsNotNull(exception);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsoluteAssetsFolderPath_Using_ForwardSlashes()
        {
            var input = Application.dataPath + "/StreamingAssets";
            const string expectedOutput = "Assets/StreamingAssets";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativeAssetsFolderPath_Using_ForwardSlashes()
        {
            const string input = "Assets/StreamingAssets";
            const string expectedOutput = "Assets/StreamingAssets";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsoluteAssetsFilePath_Using_ForwardSlashes()
        {
            var input = Application.dataPath + "/My Prefab.prefab";
            const string expectedOutput = "Assets/My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativeAssetsFilePath_Using_ForwardSlashes()
        {
            const string input = "Assets/My Prefab.prefab";
            const string expectedOutput = "Assets/My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsoluteAssetsFolderPath_Using_Backslashes()
        {
            var input = Application.dataPath + @"\StreamingAssets";
            const string expectedOutput = @"Assets\StreamingAssets";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativeAssetsFolderPath_Using_Backslashes()
        {
            const string input = @"Assets\StreamingAssets";
            const string expectedOutput = @"Assets\StreamingAssets";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsoluteAssetsFilePath_Using_Backslashes()
        {
            var input = Application.dataPath + @"\My Prefab.prefab";
            const string expectedOutput = @"Assets\My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativeAssetsFilePath_Using_Backslashes()
        {
            const string input = @"Assets\My Prefab.prefab";
            const string expectedOutput = @"Assets\My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        #endregion

        #region GetRelativePath - Packages Folder

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsolutePackagesFolderPath_Using_ForwardSlashes()
        {
            var input = PackagesAbsolutePath + "/io.coherence.sdk";
            const string expectedOutput = "Packages/io.coherence.sdk";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativePackagesFolderPath_Using_ForwardSlashes()
        {
            const string input = "Packages/io.coherence.sdk";
            const string expectedOutput = "Packages/io.coherence.sdk";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsolutePackagesFilePath_Using_ForwardSlashes()
        {
            var input = PackagesAbsolutePath + "/io.coherence.sdk/My Prefab.prefab";
            const string expectedOutput = "Packages/io.coherence.sdk/My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativePackagesFilePath_Using_ForwardSlashes()
        {
            const string input = "Packages/io.coherence.sdk/My Prefab.prefab";
            const string expectedOutput = "Packages/io.coherence.sdk/My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsolutePackagesFolderPath_Using_Backslashes()
        {
            var input = PackagesAbsolutePath + @"\io.coherence.sdk";
            const string expectedOutput = @"Packages\io.coherence.sdk";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativePackagesFolderPath_Using_Backslashes()
        {
            const string input = @"Packages\io.coherence.sdk";
            const string expectedOutput = @"Packages\io.coherence.sdk";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsolutePackagesFilePath_Using_Backslashes()
        {
            var input = PackagesAbsolutePath + @"\io.coherence.sdk\My Prefab.prefab";
            const string expectedOutput = @"Packages\io.coherence.sdk\My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativePackagesFilePath_Using_Backslashes()
        {
            const string input = @"Packages\io.coherence.sdk\My Prefab.prefab";
            const string expectedOutput = @"Packages\io.coherence.sdk\My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        #endregion

        #region GetRelativePath - Library Folder

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsoluteLibraryFolderPath_Using_ForwardSlashes()
        {
            var input = LibraryAbsolutePath + "/coherence";
            const string expectedOutput = "Library/coherence";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativeLibraryFolderPath_Using_ForwardSlashes()
        {
            const string input = "Library/coherence";
            const string expectedOutput = "Library/coherence";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsoluteLibraryFilePath_Using_ForwardSlashes()
        {
            var input = LibraryAbsolutePath + "/coherence/My Prefab.prefab";
            const string expectedOutput = "Library/coherence/My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativeLibraryFilePath_Using_ForwardSlashes()
        {
            const string input = "Library/coherence/My Prefab.prefab";
            const string expectedOutput = "Library/coherence/My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsoluteLibraryFolderPath_Using_Backslashes()
        {
            var input = LibraryAbsolutePath + @"\coherence";
            const string expectedOutput = @"Library\coherence";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativeLibraryFolderPath_Using_Backslashes()
        {
            const string input = @"Library\coherence";
            const string expectedOutput = @"Library\coherence";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_AbsoluteLibraryFilePath_Using_Backslashes()
        {
            var input = LibraryAbsolutePath + @"\coherence\My Prefab.prefab";
            const string expectedOutput = @"Library\coherence\My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetRelativePath_ShouldWorkWith_RelativeLibraryFilePath_Using_Backslashes()
        {
            const string input = @"Library\coherence\My Prefab.prefab";
            const string expectedOutput = @"Library\coherence\My Prefab.prefab";

            var actualOutput = PathUtils.GetRelativePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        #endregion

        #region GetFullPath - Assets Folder

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_NonProjectRelativePath()
        {
            const string input = "C:/My File.exe";
            const string expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_Should_ReturnProjectAbsolutePath_WhenGiven_AnEmptyString()
        {
            const string input = "";
            var expectedOutput = Paths.projectAbsolutePath;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_Should_ThrowArgumentNullException_WhenGiven_ANullString()
        {
            const string input = null;

            var exception = Assert.Catch<ArgumentNullException>(() => _ = PathUtils.GetFullPath(input));

            Assert.IsNotNull(exception);
        }

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsoluteAssetsFolderPath_Using_ForwardSlashes()
        {
            var input = Application.dataPath + "/StreamingAssets";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativeAssetsFolderPath_Using_ForwardSlashes()
        {
            const string input = "Assets/StreamingAssets";
            var expectedOutput = Application.dataPath + "/StreamingAssets";

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsoluteAssetsFilePath_Using_ForwardSlashes()
        {
            var input = Application.dataPath + "/My Prefab.prefab";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativeAssetsFilePath_Using_ForwardSlashes()
        {
            const string input = "Assets/My Prefab.prefab";
            var expectedOutput = Application.dataPath + "/My Prefab.prefab";

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsoluteAssetsFolderPath_Using_Backslashes()
        {
            var input = DataPathWithBackslashes + @"\StreamingAssets";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativeAssetsFolderPath_Using_Backslashes()
        {
            const string input = @"Assets\StreamingAssets";
            var expectedOutput = DataPathWithBackslashes + @"\StreamingAssets";

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsoluteAssetsFilePath_Using_Backslashes()
        {
            var input = DataPathWithBackslashes + @"\My Prefab.prefab";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativeAssetsFilePath_Using_Backslashes()
        {
            const string input = @"Assets\My Prefab.prefab";
            var expectedOutput = DataPathWithBackslashes + @"\My Prefab.prefab";

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        #endregion

        #region GetFullPath - Packages Folder

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsolutePackagesFolderPath_Using_ForwardSlashes()
        {
            var input = PackagesAbsolutePath + "/io.coherence.sdk";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativePackagesFolderPath_Using_ForwardSlashes()
        {
            const string input = "Packages/io.coherence.sdk/My Folder";
            var expectedOutput = Path.GetFullPath(input).Replace('\\', '/');

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
            Assert.IsTrue(actualOutput.EndsWith("/My Folder"));
        }

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsolutePackagesFilePath_Using_ForwardSlashes()
        {
            var input = PackagesAbsolutePath + "/io.coherence.sdk/My Prefab.prefab";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativePackagesFilePath_Using_ForwardSlashes()
        {
            const string input = "Packages/io.coherence.sdk/My Prefab.prefab";
            var expectedOutput = Path.GetFullPath(input).Replace('\\', '/');

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
            Assert.IsTrue(actualOutput.EndsWith("/My Prefab.prefab"));
        }

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsolutePackagesFolderPath_Using_Backslashes()
        {
            var input = PackagesPathWithBackslashes + @"\io.coherence.sdk";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativePackagesFolderPath_Using_Backslashes()
        {
            const string input = @"Packages\io.coherence.sdk\My Folder";
            var expectedOutput = Path.GetFullPath(input).Replace('/', '\\');

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
            Assert.IsTrue(actualOutput.EndsWith("\\My Folder"));
        }

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsolutePackagesFilePath_Using_Backslashes()
        {
            var input = PackagesPathWithBackslashes + @"\io.coherence.sdk\My Prefab.prefab";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativePackagesFilePath_Using_Backslashes()
        {
            const string input = @"Packages\io.coherence.sdk\My Prefab.prefab";
            var expectedOutput = Path.GetFullPath(input).Replace('/', '\\');

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
            Assert.IsTrue(actualOutput.EndsWith("\\My Prefab.prefab"));
        }

        #endregion

        #region GetFullPath - Library Folder

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsoluteLibraryFolderPath_Using_ForwardSlashes()
        {
            var input = LibraryAbsolutePath + "/coherence";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativeLibraryFolderPath_Using_ForwardSlashes()
        {
            const string input = "Library/coherence";
            var expectedOutput = LibraryAbsolutePath + "/coherence";

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsoluteLibraryFilePath_Using_ForwardSlashes()
        {
            var input = LibraryAbsolutePath + "/coherence/My Prefab.prefab";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativeLibraryFilePath_Using_ForwardSlashes()
        {
            const string input = "Library/coherence/My Prefab.prefab";
            var expectedOutput = LibraryAbsolutePath + "/coherence/My Prefab.prefab";

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsoluteLibraryFolderPath_Using_Backslashes()
        {
            var input = LibraryPathWithBackslashes + @"\coherence";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativeLibraryFolderPath_Using_Backslashes()
        {
            const string input = @"Library\coherence";
            var expectedOutput = LibraryPathWithBackslashes + @"\coherence";

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_Should_ReturnInputAsIs_WhenGiven_AbsoluteLibraryFilePath_Using_Backslashes()
        {
            var input = LibraryPathWithBackslashes + @"\coherence\My Prefab.prefab";
            var expectedOutput = input;

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetFullPath_ShouldWorkWith_RelativeLibraryFilePath_Using_Backslashes()
        {
            const string input = @"Library\coherence\My Prefab.prefab";
            var expectedOutput = LibraryPathWithBackslashes + @"\coherence\My Prefab.prefab";

            var actualOutput = PathUtils.GetFullPath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        #endregion

        #region GetMetaFilePath

        [Test]
        public void GetMetaFilePath_Should_ReturnAnEmptyString_WhenGiven_AnEmptyString()
        {
            const string input = "";
            const string expectedOutput = "";

            var actualOutput = PathUtils.GetMetaFilePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetMetaFilePath_Should_AppendMetaToExtensions_WhenGiven_AFileNameWithAnExtension()
        {
            Assert.AreEqual("Folder.meta", PathUtils.GetMetaFilePath("Folder"));
            Assert.AreEqual("File.txt.meta", PathUtils.GetMetaFilePath("File.txt"));
            Assert.AreEqual(".meta.meta", PathUtils.GetMetaFilePath(".meta"));
            Assert.AreEqual(".meta", PathUtils.GetMetaFilePath("."));
        }

        [Test]
        public void GetMetaFilePath_Should_ThrowArgumentNullException_WhenGiven_ANullString()
        {
            const string input = null;

            var exception = Assert.Catch<ArgumentNullException>(() => _ = PathUtils.GetMetaFilePath(input));

            Assert.IsNotNull(exception);
        }

        [Test]
        public void GetMetaFilePath_ShouldWorkWith_AbsoluteAssetsFolderPath_Using_ForwardSlashes()
        {
            var input = Application.dataPath + "/StreamingAssets";
            var expectedOutput = input + ".meta";

            var actualOutput = PathUtils.GetMetaFilePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetMetaFilePath_ShouldWorkWith_RelativeAssetsFolderPath_Using_ForwardSlashes()
        {
            const string input = "Assets/StreamingAssets";
            var expectedOutput = input + ".meta";

            var actualOutput = PathUtils.GetMetaFilePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetMetaFilePath_ShouldWorkWith_AbsoluteAssetsFilePath_Using_ForwardSlashes()
        {
            var input = Application.dataPath + "/My Prefab.prefab";
            var expectedOutput = input + ".meta";

            var actualOutput = PathUtils.GetMetaFilePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetMetaFilePath_ShouldWorkWith_RelativeAssetsFilePath_Using_ForwardSlashes()
        {
            const string input = "Assets/My Prefab.prefab";
            var expectedOutput = input + ".meta";

            var actualOutput = PathUtils.GetMetaFilePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetMetaFilePath_ShouldWorkWith_AbsoluteAssetsFolderPath_Using_Backslashes()
        {
            var input = DataPathWithBackslashes + @"\StreamingAssets";
            var expectedOutput = input + ".meta";

            var actualOutput = PathUtils.GetMetaFilePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetMetaFilePath_ShouldWorkWith_RelativeAssetsFolderPath_Using_Backslashes()
        {
            const string input = @"Assets\StreamingAssets";
            var expectedOutput = input + ".meta";

            var actualOutput = PathUtils.GetMetaFilePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetMetaFilePath_ShouldWorkWith_AbsoluteAssetsFilePath_Using_Backslashes()
        {
            var input = DataPathWithBackslashes + @"\My Prefab.prefab";
            var expectedOutput = input + ".meta";

            var actualOutput = PathUtils.GetMetaFilePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void GetMetaFilePath_ShouldWorkWith_RelativeAssetsFilePath_Using_Backslashes()
        {
            const string input = @"Assets\My Prefab.prefab";
            var expectedOutput = input + ".meta";

            var actualOutput = PathUtils.GetMetaFilePath(input);

            Assert.AreEqual(expectedOutput, actualOutput);
        }

        #endregion
    }
}
