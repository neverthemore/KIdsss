// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Build;
    using Connection;
    using Portal;
    using Toolkit;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;

    internal static class SimulatorGUI
    {
        private static SerializedObject buildOptionsSo;
        private static string selectedRegion = EndpointData.LocalRegion;
        private static float minWindowWidth = 285f;

        internal static void DrawSimulatorBuildOptions(SimulatorBuildOptions buildOptions, float windowWidth)
        {
            if (buildOptionsSo == null)
            {
                buildOptionsSo = new SerializedObject(buildOptions);
            }

            var headlessMode = buildOptionsSo.FindProperty("headlessMode");
            var scriptingImplementation = buildOptionsSo.FindProperty("scriptingImplementation");
            var developmentBuild = buildOptionsSo.FindProperty("developmentBuild");
            var buildSizeOptimizations = buildOptionsSo.FindProperty("buildSizeOptimizations");
            var replaceTextures = buildSizeOptimizations.FindPropertyRelative("stripAssets");
            var keepOriginalAssetsBackup = buildSizeOptimizations.FindPropertyRelative("backupAssets");
            var compressMeshes = buildSizeOptimizations.FindPropertyRelative("compressMeshes");
            var disableStaticBatching = buildSizeOptimizations.FindPropertyRelative("disableStaticBatching");

            var headlessModeContent = new GUIContent(headlessMode.displayName,
                "This option will always be true for Linux clients that are built to be uploaded to the Portal");
            var scriptingImplementationContent = new GUIContent(scriptingImplementation.displayName);
            var developmentBuildContent = new GUIContent(developmentBuild.displayName);
            var buildSizeOptimizationsContent = new GUIContent(buildSizeOptimizations.displayName);
            var replaceTexturesContent = new GUIContent(replaceTextures.displayName);
            var keepOriginalAssetsBackupContent = new GUIContent(keepOriginalAssetsBackup.displayName);
            var compressMeshessContent = new GUIContent(compressMeshes.displayName);
            var disableStaticBatchingContent = new GUIContent(disableStaticBatching.displayName);

            var maxWidth = CoherenceHubLayout.Styles.Label.CalcSize(headlessModeContent).x + 20f;
            // Extremely ugly hack to make the build options scale and wrap properly depending on window width
            maxWidth += windowWidth > minWindowWidth ? windowWidth - minWindowWidth : 0f;

            EditorGUILayout.PropertyField(buildOptionsSo.FindProperty("scenesToBuild"));

            DrawToggle(headlessModeContent, headlessMode, maxWidth);
            DrawGenericProperty(scriptingImplementationContent, scriptingImplementation, maxWidth);
            DrawToggle(developmentBuildContent, developmentBuild, maxWidth);

            EditorGUILayout.Separator();

            CoherenceHubLayout.DrawBoldLabel(buildSizeOptimizationsContent);
            DrawToggle(replaceTexturesContent, replaceTextures, maxWidth);
            DrawToggle(keepOriginalAssetsBackupContent, keepOriginalAssetsBackup, maxWidth);
            DrawToggle(compressMeshessContent, compressMeshes, maxWidth);
            DrawToggle(disableStaticBatchingContent, disableStaticBatching, maxWidth);

            buildOptionsSo.ApplyModifiedProperties();
        }

        private static void DrawGenericProperty(GUIContent content, SerializedProperty prop, float maxWidth)
        {
            using var scope = new EditorGUILayout.HorizontalScope(GUILayout.Width(maxWidth + 90f));
            CoherenceHubLayout.DrawLabel(content, null, GUILayout.Width(maxWidth));
            EditorGUILayout.PropertyField(prop, new GUIContent(string.Empty));
        }

        private static void DrawToggle(GUIContent content, SerializedProperty prop, float maxWidth)
        {
            using var scope = new EditorGUILayout.HorizontalScope(GUILayout.Width(maxWidth));
            CoherenceHubLayout.DrawLabel(content, null, GUILayout.Width(maxWidth));
            prop.boolValue = CoherenceHubLayout.DrawToggle(string.Empty, prop.boolValue, null, GUILayout.Width(40f));
        }

        internal static void DrawCreateAndUploadHeadlessSimulatorBuild(SimulatorBuildOptions buildOptions)
        {
            CoherenceHubLayout.DrawInfoLabel(new GUIContent(
                "Simulator build will be uploaded to your selected organization and project in the coherence Cloud."));
            EditorGUILayout.Space();

            DrawSimSlug();

            var isHeadlessBuildSupported =
                SimulatorEditorUtility.IsBuildTargetSupported(true, buildOptions.ScriptingImplementation);
            var isNonHeadlessBuildSupported =
                SimulatorEditorUtility.IsBuildTargetSupported(false, buildOptions.ScriptingImplementation);

            DrawCreateAndUploadBuildButton(buildOptions, isHeadlessBuildSupported || isNonHeadlessBuildSupported);
            DrawCreateAndUploadErrorMessages(buildOptions, isHeadlessBuildSupported, isNonHeadlessBuildSupported);
            DrawCreateAndUploadInformation(isHeadlessBuildSupported);
        }

        internal static void DrawLocalSimulatorBuild(SimulatorBuildOptions buildOptions)
        {
            CoherenceHubLayout.DrawInfoLabel(new GUIContent(
                "Simulator build for your active build target, the COHERENCE_SIMULATOR symbol will be added."));
            CoherenceHubLayout.DrawMessageArea(new GUIContent("You can use this build to test it locally."));
            EditorGUILayout.Space();

            DrawLocalSimulatorBuildButton(buildOptions);
            DrawLocalBuildErrorMessages(buildOptions);
            DrawLocalBuildInformation(buildOptions);
        }

        internal static void DrawAutoSimulatorConnection()
        {
            CoherenceHubLayout.DrawMessageArea(
                new GUIContent("Add this Component to let Simulators connect to RS automatically."));
            EditorGUILayout.Space();

            if (GUILayout.Button("Add AutoSimulatorConnection"))
            {
                Utils.AddAutoSimulatorConnection(null);
            }
        }

        internal static void DrawRunSimulatorSettings(ref EndpointData endpoint)
        {
            DrawExecutablePath();

            EditorGUILayout.Space();

            DrawReplicationServerDataHeader(ref endpoint);

            EditorGUILayout.Space();

            InitRegionAndSimType(ref endpoint);

            RegionsGenericMenu(endpoint.region);

            EditorGUI.BeginChangeCheck();

            endpoint.region = selectedRegion;
            endpoint.simulatorType = EditorGUILayout.EnumPopup(new GUIContent("Type"),
                ParseEnumValue(typeof(EndpointData.SimulatorType), endpoint.simulatorType)).ToString();
            endpoint.host = CoherenceHubLayout.DrawTextField("Host", endpoint.host);
            endpoint.port = CoherenceHubLayout.DrawIntField("Port", endpoint.port);
            endpoint.worldId = UInt64.Parse(CoherenceHubLayout.DrawTextField("World Id", endpoint.worldId.ToString()));
            endpoint.roomId = (ushort)CoherenceHubLayout.DrawIntField("Room Id", endpoint.roomId);

            if (!ulong.TryParse(CoherenceHubLayout.DrawTextField("Unique Room Id", endpoint.uniqueRoomId.ToString()),
                    out endpoint.uniqueRoomId))
            {
                endpoint.uniqueRoomId = 0;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SimulatorEditorUtility.SaveEndPoint(endpoint);
            }

            DrawRunLocalSimulatorButton(endpoint);

            DrawRunSimErrorMessages(endpoint);
        }

        private static void RegionsGenericMenu(string currentRegion)
        {
            using var scope = new EditorGUILayout.HorizontalScope();

            CoherenceHubLayout.DrawPrefixLabel(new GUIContent("Region"));
            var buttonText = currentRegion.Equals(EndpointData.LocalRegion)
                ? "Local"
                : currentRegion.ToUpperInvariant();

            var nextRect = EditorGUILayout.GetControlRect(false);

            if (CoherenceHubLayout.DrawButtonAsPopup(nextRect, buttonText))
            {
                var regionsMenu = new GenericMenu();

                foreach (var region in PortalLogin.availableRegionsForCurrentProject)
                {
                    if (region.Equals(EndpointData.LocalRegion))
                    {
                        regionsMenu.AddItem(new GUIContent("Local"), currentRegion.Equals(region),
                            () => ChangeSelectedRegion(region));
                    }
                    else
                    {
                        regionsMenu.AddDisabledItem(new GUIContent(region.ToUpperInvariant()),
                            currentRegion.Equals(region));
                    }
                }

                regionsMenu.DropDown(nextRect);
            }
        }

        private static void ChangeSelectedRegion(string newRegion)
        {
            selectedRegion = newRegion;
        }

        private static void InitRegionAndSimType(ref EndpointData endpoint)
        {
            if (string.IsNullOrEmpty(endpoint.region))
            {
                endpoint.region = EndpointData.LocalRegion;
            }

            if (string.IsNullOrEmpty(endpoint.simulatorType))
            {
                endpoint.simulatorType = EndpointData.SimulatorType.world.ToString();
            }
        }

        private static Enum ParseEnumValue(Type type, string value)
        {
            return (Enum)Enum.Parse(type, value);
        }

        private static bool IsRegionSupported(string region)
        {
            return region.Equals(EndpointData.LocalRegion);
        }

        private static void DrawRunLocalSimulatorButton(EndpointData endpoint)
        {
            EditorGUI.BeginDisabledGroup(!SimulatorEditorUtility.CanRunLocalSimulator(endpoint));
            using (new EditorGUILayout.HorizontalScope())
            {
                var content = Icons.GetContentWithText("Coherence.Terminal", "Run Local Simulator",
                    "Execute a local simulator Unity build, and automatically connect to the given Replication Server.");
                if (GUILayout.Button(content, EditorStyles.miniButtonLeft))
                {
                    SimulatorEditorUtility.RunLocalSimulator(SimulatorEditorUtility.LocalSimulatorExecutablePath,
                        endpoint);
                }

                if (GUILayout.Button(LocalServerModule.ModuleGUIContents.Clipboard, EditorStyles.miniButtonRight,
                        GUILayout.Width(22)))
                {
                    GUIUtility.systemCopyBuffer = SimulatorEditorUtility.GetLocalSimCommand(
                        SimulatorEditorUtility.LocalSimulatorExecutablePath,
                        endpoint);
                    EditorWindow.focusedWindow.ShowNotification(LocalServerModule.ModuleGUIContents.Copied);
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        private static void DrawReplicationServerDataHeader(ref EndpointData endpoint)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawBoldLabel(new GUIContent("Replication Server endpoint"));
                GUILayout.FlexibleSpace();

                var guiContent = new GUIContent("Fetch",
                    "The RS Endpoint Data will be automatically fetched from the last RS you connected to in runtime.");

                if (CoherenceHubLayout.DrawButton(guiContent))
                {
                    if (SimulatorEditorUtility.LastUsedEndpoint.Validate().isValid &&
                        !string.IsNullOrEmpty(endpoint.region)
                        && IsRegionSupported(endpoint.region))
                    {
                        endpoint = SimulatorEditorUtility.LastUsedEndpoint;
                        endpoint.host = endpoint.host.Trim();

                        if (endpoint.worldId != 0)
                        {
                            endpoint.simulatorType = EndpointData.SimulatorType.world.ToString();
                        }
                        else if (endpoint.roomId != 0 || endpoint.uniqueRoomId != 0)
                        {
                            endpoint.simulatorType = EndpointData.SimulatorType.room.ToString();
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(endpoint.region))
                        {
                            Debug.LogWarning("No last used RS endpoint data available");
                        }
                        else if (!IsRegionSupported(endpoint.region))
                        {
                            Debug.LogWarning(
                                $"Region {endpoint.region} is not supported right now. Stay tuned for updates!");
                        }
                    }
                }
            }
        }
        private static void DrawExecutablePath()
        {
            var previousPath = SimulatorEditorUtility.LocalSimulatorExecutablePath;
            CoherenceHubLayout.DrawDiskPath(SimulatorEditorUtility.LocalSimulatorExecutablePath,
                "Select a valid Simulator executable file.", OpenFilePanel, newPath =>
                {
                    newPath = FixMacOsPath(newPath);
                    if (!newPath.EndsWith(SimulatorBuildOptions.BuildName))
                    {
                        newPath = previousPath;
                        EditorUtility.DisplayDialog("Invalid Simulator Executable",
                            "The selected file is not a valid Simulator executable.", "Ok");
                    }

                    SimulatorEditorUtility.LocalSimulatorExecutablePath = newPath;
                });
        }

        private static string FixMacOsPath(string newPath)
        {
            if (!newPath.EndsWith(".app"))
            {
                return newPath;
            }

            var pathCandidate = $"{newPath}/Contents/MacOS/{SimulatorBuildOptions.BuildName}";

            if (!File.Exists(pathCandidate))
            {
                pathCandidate = $"{newPath}/Contents/MacOS/{PlayerSettings.productName}";
            }

            newPath = pathCandidate;

            return newPath;
        }

        private static string OpenFilePanel()
        {
            return EditorUtility.OpenFilePanel("Select Simulator Build Executable", string.Empty, GetExtension());
        }

        private static string GetExtension()
        {
#if UNITY_EDITOR_WIN
            return "exe";
#else
            return string.Empty;
#endif
        }

        private static void DrawRunSimErrorMessages(EndpointData endpoint)
        {
            if (!SimulatorEditorUtility.ValidateIpAndPort(endpoint))
            {
                CoherenceHubLayout.DrawWarnArea("Endpoint is missing host or port");
            }

            if (!SimulatorEditorUtility.ValidateIp(endpoint.host))
            {
                CoherenceHubLayout.DrawWarnArea("Endpoint IP is invalid");
            }

            if (!SimulatorEditorUtility.BuildPathHasSimulatorBuild())
            {
                CoherenceHubLayout.DrawWarnArea("Build path doesn't contain a Simulator build");
            }
        }

        private static void DrawSimSlug()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawPrefixLabel(new GUIContent("Simulator Slug",
                    "The simulator slug is a string that uniquely identifies this upload. It can be used later when deploying world or room simulators."));
                RuntimeSettings.Instance.SimulatorSlug =
                    CoherenceHubLayout.DrawTextField(RuntimeSettings.Instance.SimulatorSlug);
            }
        }

        private static void DrawCreateAndUploadInformation(bool isHeadlessBuildSupported)
        {
            bool isLinux = EditorUserBuildSettings.selectedStandaloneTarget == BuildTarget.StandaloneLinux64;

            if (!isLinux)
            {
                CoherenceHubLayout.DrawMessageArea("Linux platform");
            }

            bool isServerSubTarget =
                EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server;

            if (!isServerSubTarget && isHeadlessBuildSupported)
            {
                CoherenceHubLayout.DrawMessageArea("Standalone: Server module");
            }
            else if (isServerSubTarget & !isHeadlessBuildSupported)
            {
                CoherenceHubLayout.DrawMessageArea("Standalone: Player");
            }

            bool hasCoherenceSimulatorDefine = PlayerSettings
                .GetScriptingDefineSymbols(isHeadlessBuildSupported
                    ? NamedBuildTarget.Server
                    : NamedBuildTarget.Standalone).Contains("COHERENCE_SIMULATOR");

            if (!hasCoherenceSimulatorDefine)
            {
                CoherenceHubLayout.DrawMessageArea("COHERENCE_SIMULATOR scripting symbol");
            }
        }

        private static void DrawLocalBuildInformation(SimulatorBuildOptions buildOptions)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);

            var namedBuildTarget = buildOptions.HeadlessMode
                ? NamedBuildTarget.Server
                : NamedBuildTarget.FromBuildTargetGroup(group);
            bool hasCoherenceSimulatorDefine = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget)
                .Contains("COHERENCE_SIMULATOR");

            if (!hasCoherenceSimulatorDefine)
            {
                CoherenceHubLayout.DrawMessageArea("COHERENCE_SIMULATOR scripting symbol");
            }
        }

        private static void DrawCreateAndUploadErrorMessages(SimulatorBuildOptions buildOptions,
            bool isHeadlessBuildSupported, bool isNonHeadlessBuildSupported)
        {
            if (string.IsNullOrEmpty(RuntimeSettings.Instance.SimulatorSlug))
            {
                CoherenceHubLayout.DrawErrorArea("Simulator Slug string is empty");
            }

            if (!buildOptions.CanBuild)
            {
                CoherenceHubLayout.DrawErrorArea("Simulator build options need at least one scene");
            }

            if (!PortalUtil.CanCommunicateWithPortal)
            {
                CoherenceHubLayout.DrawErrorArea("Log in to be able to upload builds");
            }

            if (!isHeadlessBuildSupported && isNonHeadlessBuildSupported)
            {
                CoherenceHubLayout.DrawWarnArea(
                    $"Headless build not supported for {buildOptions.ScriptingImplementation}, install the Linux Dedicated Server module");
            }
            else if (!isHeadlessBuildSupported && !isNonHeadlessBuildSupported)
            {
                CoherenceHubLayout.DrawErrorArea(
                    $"Linux build not supported for {buildOptions.ScriptingImplementation}, install the Linux modules.");
            }

            EditorImportingOrCompilingWarning();
        }

        private static void DrawLocalBuildErrorMessages(SimulatorBuildOptions buildOptions)
        {
            if (!buildOptions.CanBuild)
            {
                CoherenceHubLayout.DrawErrorArea("Simulator build options need at least one scene");
            }

            EditorImportingOrCompilingWarning();
        }

        private static void EditorImportingOrCompilingWarning()
        {
            if (IsEditorImportingOrCompiling())
            {
                CoherenceHubLayout.DrawWarnArea(
                    "Cannot build player while editor is importing assets or compiling scripts.");
            }
        }

        private static bool IsEditorImportingOrCompiling()
        {
            return EditorApplication.isCompiling || EditorApplication.isUpdating;
        }

        private static void DrawCreateAndUploadBuildButton(SimulatorBuildOptions buildOptions, bool canBuildLinux)
        {
            using var scope = new EditorGUI.DisabledScope(DisableConditions(buildOptions, canBuildLinux));
            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawCloudDependantButton(new GUIContent("Build And Upload Headless Linux Client"),
                    () =>
                    {
                        SimulatorBuildPipeline.BuildHeadlessLinuxClientAsync();
                        GUIUtility.ExitGUI();
                    }, string.Empty);

                if (GUILayout.Button(LocalServerModule.ModuleGUIContents.HeadlessBuildExecutable,
                        EditorStyles.miniButtonRight,
                        GUILayout.Width(22)))
                {
                    var path = Shellscript.ExportSimulatorBuildCommand();
                    EditorUtility.RevealInFinder(path);
                }
            }
        }

        private static bool DisableConditions(SimulatorBuildOptions buildOptions, bool canBuildLinux)
        {
            return !buildOptions.CanBuild || !canBuildLinux || IsEditorImportingOrCompiling();
        }

        private static void DrawLocalSimulatorBuildButton(SimulatorBuildOptions buildOptions)
        {
            using (new EditorGUI.DisabledScope(!buildOptions.CanBuild || IsEditorImportingOrCompiling()))
            {
                var forcePrompt = Event.current.alt;
                var hasPreviousPath =
                    !string.IsNullOrEmpty(SessionState.GetString(SimulatorBuildPipeline.PreviousLocalBuildPathKey,
                        string.Empty));
                var buildText = hasPreviousPath && !forcePrompt ? "Rebuild" : "Build";
                var altKey = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Option" : "Alt";
                var buildSimulatorLabel =
                    new GUIContent($"{buildText} Local Simulator ({EditorUserBuildSettings.activeBuildTarget})")
                    {
                        tooltip = hasPreviousPath
                            ? $"{buildText} the Local Simulator for {EditorUserBuildSettings.activeBuildTarget}. Hold down the {altKey} key when clicking this button to specify a new build path."
                            : "Build a simulator to test locally.",
                    };

                if (CoherenceHubLayout.DrawButton(buildSimulatorLabel))
                {
                    // If the user holds down alt, show the build location dialog
                    SimulatorBuildPipeline.BuildLocalSimulator(forcePrompt);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}
