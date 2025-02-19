// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Coherence.Tests;
    using NUnit.Framework;
    using UnityEditor;

    public class ReplicationServerBundlerTests : CoherenceTest
    {
        [Test]
        public void DeleteRsFromStreamingAssets_Should_DeleteReplicationServerFileCreatedBy_BundleWithStreamingAssets_ForAllSupportedPlatforms()
        {
            var supportedPlatforms = ReplicationServerBinaries.GetSupportedPlatforms();
            var skippedPlatforms = new List<BuildTarget>();
            foreach (var buildTarget in supportedPlatforms)
            {
                var skipped = !DeleteRsFromStreamingAssets_Should_DeleteReplicationServerFileCreatedBy_BundleWithStreamingAssets(buildTarget);
                if (skipped)
                {
                    skippedPlatforms.Add(buildTarget);
                }
            }

            if (skippedPlatforms.Any())
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("DeleteRsFromStreamingAssets test was skipped for the following platforms:");
                foreach (var buildTarget in skippedPlatforms)
                {
                    stringBuilder.Append("\n");
                    stringBuilder.Append(buildTarget);
                    stringBuilder.Append(" (binary not found at ");
                    stringBuilder.Append(ReplicationServerBinaries.GetToolsPath(buildTarget));
                    stringBuilder.Append(").");
                }

                stringBuilder.Append("\n\nThis is not unusual; with dev testing it's common to install only OS-related bundle for development.");
                Assert.Ignore(stringBuilder.ToString());
            }
        }

        /// <returns>
        /// true if replication server file for the platform was found, and was able to perform the test; otherwise, false.
        /// </returns>
        private bool DeleteRsFromStreamingAssets_Should_DeleteReplicationServerFileCreatedBy_BundleWithStreamingAssets(BuildTarget buildTarget)
        {
            var sourcePath = ReplicationServerBinaries.GetToolsPath(buildTarget);
            if (!sourcePath.HasFile())
            {
                return false;
            }

            var destinationPath = ReplicationServerBinaries.GetStreamingAssetsPath(buildTarget);
            AssetPath metaFilePath = PathUtils.GetMetaFilePath(destinationPath);
            ReplicationServerBundler.BundleWithStreamingAssets(buildTarget);

            var fileExisted = destinationPath.HasFile();
            var metaFileExisted = metaFilePath.HasFile();

            var wasDeleted = ReplicationServerBundler.DeleteRsFromStreamingAssets(buildTarget);

            Assert.IsTrue(fileExisted, "BundleWithStreamingAssets failed for platform {0} at {1}.", buildTarget, destinationPath);
            Assert.IsTrue(metaFileExisted, "BundleWithStreamingAssets failed to create .meta file synchronously for platform {0} at {1}.", buildTarget, metaFilePath);
            Assert.IsTrue(wasDeleted, "DeleteRsFromStreamingAssets failed for platform {0} at {1}.", buildTarget, destinationPath);
            Assert.IsFalse(destinationPath.HasFile(), "DeleteRsFromStreamingAssets failed to delete executable file for platform {0} at {1}.", buildTarget, destinationPath, destinationPath.ToFullPath());
            Assert.IsFalse(metaFilePath.HasFile(), "DeleteRsFromStreamingAssets failed to delete .meta file for platform {0} at {1}.", buildTarget, destinationPath, destinationPath.ToFullPath());

            AssetUtils.DeleteFolderIfEmpty(Paths.streamingAssetsPath);
            return true;
        }
    }
}
