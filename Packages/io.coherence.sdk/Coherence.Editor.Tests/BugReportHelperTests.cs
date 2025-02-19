// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor.Tests
{
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using Coherence.Tests;
    using NUnit.Framework;
    using UnityEditor;
    using static BugReportHelper;

    /// <summary>
    /// Unit tests for <see cref="BugReportHelper"/>.
    /// </summary>
    public sealed class BugReportHelperTests : CoherenceTest
    {
        [Test]
        public void CreateZipFile_Succeeds_If_Given_Valid_OutputPath()
        {
            var outputPath = FileUtil.GetUniqueTempPathInProject();
            using var result = BugReportHelper.CreateZipFile(outputPath, true, InteractionMode.AutomatedAction);
            Assert.That(result.Type, Is.EqualTo(CreateZipFileResultType.Succeeded));
        }

        [TestCase(default(string)), TestCase(""), TestCase("c:\\")]
        public void CreateZipFile_Fails_If_Given_Invalid_OutputPath(string outputPath)
        {
            using var result = BugReportHelper.CreateZipFile(outputPath, true, InteractionMode.AutomatedAction);
            Assert.That(result.Type, Is.EqualTo(CreateZipFileResultType.Failed));
        }

        [TestCase(default(string)), TestCase(""), TestCase("c:\\")]
        public void CreateZipFile_Contains_One_Or_More_Exceptions_If_Given_Invalid_OutputPath(string outputPath)
        {
            using var result = BugReportHelper.CreateZipFile(outputPath, true, InteractionMode.AutomatedAction);
            Assert.That(result.Exceptions.Count(), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void CreateZipFile_Result_Contains_Path_To_Created_ZipFile_If_Succeeds()
        {
            var outputPath = FileUtil.GetUniqueTempPathInProject();
            using var result = BugReportHelper.CreateZipFile(outputPath, true, InteractionMode.AutomatedAction);
            Assert.That(result.CreatedFilePath, Is.Not.Null.And.Not.Empty);
            Assert.That(File.Exists(result.CreatedFilePath), Is.True);
        }

        [Test]
        public void CreateZipFile_File_Contains_Editor_Logs_And_RuntimeSettings()
        {
            var outputPath = FileUtil.GetUniqueTempPathInProject();
            using var result = BugReportHelper.CreateZipFile(outputPath, true, InteractionMode.AutomatedAction);

            var runtimeSettingsPath = AssetDatabase.GetAssetPath(RuntimeSettings.Instance);
            var entries = new []
            {
                Path.GetFileName(runtimeSettingsPath),
                Path.GetFileName(Paths.CurrentEditorLogFileAbsolutePath),
            };

            var allFilesPresent = DoesZipFileContainAllFiles(outputPath, entries);
            Assert.That(allFilesPresent, Is.True);

            Assert.Ignore($"Skipping remainder of test {nameof(CreateZipFile_File_Contains_Editor_Logs_And_RuntimeSettings)}, remove this Ignore() after https://github.com/coherence/unity/issues/6623");
            var dupesDetected = DoesZipContainDuplicates(outputPath, entries);
            Assert.That(dupesDetected, Is.False);
        }

        [Test]
        public void CreateZipFile_File_Contains_RuntimeSettings_No_Editor_Logs()
        {
            var outputPath = FileUtil.GetUniqueTempPathInProject();
            using var result = BugReportHelper.CreateZipFile(outputPath, false, InteractionMode.AutomatedAction);

            var runtimeSettingsPath = AssetDatabase.GetAssetPath(RuntimeSettings.Instance);
            var entries = new []
            {
                Path.GetFileName(runtimeSettingsPath)
            };

            var allFilesPresent = DoesZipFileContainAllFiles(outputPath, entries);
            Assert.That(allFilesPresent, Is.True);

            Assert.Ignore($"Skipping remainder of test {nameof(CreateZipFile_File_Contains_RuntimeSettings_No_Editor_Logs)}, remove this Ignore() after https://github.com/coherence/unity/issues/6623");
            var dupesDetected = DoesZipContainDuplicates(outputPath, entries);
            Assert.That(dupesDetected, Is.False);
        }

        private bool DoesZipFileContainAllFiles(string archiveFileName, string[] filenames)
        {
            using var zipFile = ZipFile.OpenRead(archiveFileName);
            var intersections = zipFile.Entries.Select(e => e.Name).Intersect(filenames);
            return intersections.Count() == filenames.Length;
        }

        private bool DoesZipContainDuplicates(string archiveFileName, string[] filenames)
        {
            using var zipFile = ZipFile.OpenRead(archiveFileName);
            foreach (var filename in filenames)
            {
                var count = zipFile.Entries.Count(e => e.Name == filename);
                if (count > 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
