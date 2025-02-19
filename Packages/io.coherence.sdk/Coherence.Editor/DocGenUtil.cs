namespace Coherence.Editor
{
    using UnityEngine;
    using System.IO;

    internal static class DocGenUtil
    {
        public static bool HasDirectoryBuildTargets => File.Exists(Paths.directoryBuildTargetsPath);

        public static void GenerateDirectoryBuildTargets()
        {
            using var stream = File.CreateText(Paths.directoryBuildTargetsPath);
            stream.Write(
$@"<Project>
  <PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <DocumentationFile>{Paths.xmlDocsRelativePath}/$(AssemblyName).xml</DocumentationFile>
  </PropertyGroup>
</Project>");
        }

        public static void FetchBuildArtifacts()
        {
            _ = Directory.CreateDirectory(Paths.docFxDllsPath);
            foreach (var file in Directory.EnumerateFiles(Path.GetFullPath(Paths.docFxDllsPath)))
            {
                File.Delete(file);
            }

            // dlls

            foreach (var file in Directory.EnumerateFiles(Path.GetFullPath(Paths.scriptAssembliesPath), "*.dll"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!IncludeArtifact(name))
                {
                    continue;
                }

                var srcPath = file;
                var destPath = Path.GetFullPath(Path.Combine(Paths.docFxDllsPath, Path.GetFileName(srcPath)));
                File.Copy(srcPath, destPath, true);
            }

            // xml docs

            _ = Directory.CreateDirectory(Paths.xmlDocsAbsolutePath);
            foreach (var file in Directory.EnumerateFiles(Paths.xmlDocsAbsolutePath, "*.xml"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!IncludeArtifact(name))
                {
                    continue;
                }

                var srcPath = file;
                var destPath = Path.GetFullPath(Path.Combine(Paths.docFxDllsPath, Path.GetFileName(srcPath)));
                File.Copy(srcPath, destPath, true);
            }
        }

        public static void RunBuildSolution()
        {
            if (Application.isBatchMode)
            {
                throw new System.NotSupportedException();
            }

            if (Directory.Exists(Paths.xmlDocsAbsolutePath))
            {
                Directory.Delete(Paths.xmlDocsAbsolutePath, true);
            }

            ProcessUtil.RunInTerminal("dotnet", $"build \"{GetProjectFolderName()}.sln\"");
        }

        public static void RunDocFx()
        {
            if (Application.isBatchMode)
            {
                throw new System.NotSupportedException();
            }

            ProcessUtil.RunInTerminal($"docfx {Path.GetFullPath(Paths.docFxConfigPath)} --serve");
        }

        private static bool IncludeArtifact(string name) =>
            name.StartsWith("Coherence.") &&
            !name.EndsWith(".Tests") &&
            !name.EndsWith(".Generated") &&
            !name.Contains("CodeSamples");

        private static string GetProjectFolderName() => new DirectoryInfo(Paths.projectAbsolutePath).Name;
    }
}
