// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Log;
    using Portal;
    using ReplicationServer;
    using Toolkit;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [CustomEditor(typeof(ProjectSettings))]
    internal class ProjectSettingsEditor : Editor
    {
        private SchemaAsset[] schemas;
        private GUIContent[] schemaContents;
        private GUIContent[] schemaContext;
        private GUIContent schemaBakeFolderContent;

        private SerializedProperty worldUDPPort;
        private SerializedProperty worldWebPort;
        private SerializedProperty roomsUDPPort;
        private SerializedProperty roomsWebPort;
        private SerializedProperty sendFrequency;
        private SerializedProperty recvFrequency;
        private SerializedProperty localRoomsCleanupTimeSeconds;
        private SerializedProperty rsConsoleLogLevel;
        private SerializedProperty rsLogToFile;
        private SerializedProperty rsLogFilePath;
        private SerializedProperty rsFileLogLevel;
        private SerializedProperty useToolkit;
        private SerializedProperty useCustomSchemas;
        private SerializedProperty useCoherenceSyncSchema;
        private SerializedProperty keepConnectionAlive;
        private SerializedProperty reportAnalytics;

        private RuntimeSettings runtimeSettings;
        private SerializedObject runtimeSettingsSerializedObject;
        private SerializedProperty localDevelopmentMode;
        private SerializedProperty useNativeCore;

        private SerializedProperty showHubSectionInfo;
        private SerializedProperty showHubModuleDescription;
        private SerializedProperty showHubMessageAreas;

        private string bakeFolder;

        private EditorWindow projectSettingsWindow;

        private bool lastAdvanced;
        private bool skipLongUnitTests;

        private enum Mode
        {
            Rooms,
            Worlds
        }

        private Mode mode;
        private const string modeSessionKey = "Coherence.Settings.Mode";
        private bool advanced;

        private class GUIContents
        {
            public static readonly GUIContent bakeHeader = EditorGUIUtility.TrTextContent("Baking / Code Generation");

            public static readonly GUIContent bakeDescription =
                EditorGUIUtility.TrTextContent("Create C# code for performant networking based off schemas.");

            public static readonly GUIContent bakeFolder =
                EditorGUIUtility.TrTextContent("Output Folder", "Folder where network code will be generated.");

            public static readonly GUIContent schemasHeader = EditorGUIUtility.TrTextContent("Schemas");

            public static readonly GUIContent schemasDescription = EditorGUIUtility.TrTextContent(
                "Schemas are human-readable files that define the entities to be networked. They are needed to start a replication server. Schemas are also used to create code to use for performant networking.");

            public static readonly GUIContent projectName =
                EditorGUIUtility.TrTextContent("Project Name", "Name of project in Portal");

            public static readonly GUIContent port = EditorGUIUtility.TrTextContent("Port");

            public static readonly GUIContent webPort =
                EditorGUIUtility.TrTextContent("Web Port", "Port used by default on WebGL builds.");

            public static readonly GUIContent localDevelopmentMode = EditorGUIUtility.TrTextContent(
                "Local Development Mode",
                "Allows development features like localhost replication server discovery and multi-room simulators local forwarning. Disable this on release/distributable builds.");

            public static readonly GUIContent advancedBakeText =
                Icons.GetContentWithText("Coherence.RunCommand", "Bake Wizard");

            public static readonly GUIContent replicationServer =
                EditorGUIUtility.TrTextContent("Local Replication Server");

            public static readonly GUIContent coherenceSyncSupportLabel =
                EditorGUIUtility.TrTextContent("Generic Schema", "Required by CoherenceSync.");

            public static readonly GUIContent useCustomSchemasLabel =
                EditorGUIUtility.TrTextContent("Custom Schemas...");

            public static readonly GUIContent gatheredSchemasLabel =
                EditorGUIUtility.TrTextContent("CoherenceSync Schema",
                    "Schema created reading through the active CoherenceSync prefabs.");

            public static readonly GUIContent keepConnectionAliveLabel =
                EditorGUIUtility.TrTextContent("Disable Connection Timeout (Editor Only)");

            public static readonly GUIContent reportAnalytics =
                EditorGUIUtility.TrTextContent("Share Anonymous Analytics");

            public static readonly GUIContent versionInfo = EditorGUIUtility.TrTextContent("Version Info");

            public static readonly GUIContent advanced = EditorGUIUtility.TrTextContent("Advanced");
            public static readonly GUIContent useCustomTools = EditorGUIUtility.TrTextContent("Use Custom Tools");

            public static readonly GUIContent useCustomEndpoints =
                EditorGUIUtility.TrTextContent("Use Custom Endpoints");

            public static readonly GUIContent customToolsPath = EditorGUIUtility.TrTextContent("Custom Tools Path");
            public static readonly GUIContent customAPIDomain = EditorGUIUtility.TrTextContent("Custom API Domain");

            public static readonly GUIContent useNativeCore = EditorGUIUtility.TrTextContent("Use Native Client Core");

            public static readonly GUIContent
                consoleLogLevel = EditorGUIUtility.TrTextContent("Console Level (Editor)");

            public static readonly GUIContent editorLogLevel = EditorGUIUtility.TrTextContent("Editor.log Level");

            public static readonly GUIContent logStackTrace = EditorGUIUtility.TrTextContent("Log stack trace");

            public static readonly GUIContent logSourceFilterCSV =
                EditorGUIUtility.TrTextContent("Filter by Source (comma separated)");

            public static readonly GUIContent logToFile = EditorGUIUtility.TrTextContent("Log to file");
            public static readonly GUIContent logFilePath = EditorGUIUtility.TrTextContent("Log file path");
            public static readonly GUIContent fileLogLevel = EditorGUIUtility.TrTextContent("File log level");

            public static readonly GUIContent showHubHelpInfo = EditorGUIUtility.TrTextContent("Section Info",
                "Shows a small descriptive text in each module in coherence Hub");

            public static readonly GUIContent showHubModuleDescription =
                EditorGUIUtility.TrTextContent("Tab Description",
                    "Shows a few lines of text to explain each section in coherence Hub");

            public static readonly GUIContent showHubMessages =
                EditorGUIUtility.TrTextContent("Info Messages", "Show small notification messages in coherence Hub");

            public static readonly GUIContent codegenCommand =
                EditorGUIUtility.TrTextContent("Command", "Codegen command used internally to generate sources.");

            public static readonly GUIContent logsHeader = EditorGUIUtility.TrTextContent("Logs");
            public static readonly GUIContent helpTextHeader = EditorGUIUtility.TrTextContent("Hub");

            public static readonly GUIContent helpTextDescription =
                EditorGUIUtility.TrTextContent("Information to display in the coherence Hub.");

            public static readonly GUIContent bake = EditorGUIUtility.TrTextContent("Bake");
            public static readonly GUIContent bakeNow = EditorGUIUtility.TrTextContent("Bake *");

            public static readonly GUIContent useCustomDocsEndpoint =
                EditorGUIUtility.TrTextContent("Use Custom Docs Endpoint");

            public static readonly GUIContent docsEndpoint = EditorGUIUtility.TrTextContent("Docs Endpoint");

            public static readonly GUIContent replicationServerBundlingHeader =
                EditorGUIUtility.TrTextContent("Bundle Replication Server In Build");

#if UNITY_EDITOR_OSX
            public static readonly GUIContent altToAdvanced =
                EditorGUIUtility.TrTextContent("Hold ⌥ Option to show advanced options.");
#else
            public static readonly GUIContent altToAdvanced =
                EditorGUIUtility.TrTextContent("Hold Alt to show advanced options.");
#endif

#if UNITY_EDITOR_OSX
            public static readonly GUIContent altToBuildLogs =
                EditorGUIUtility.TrTextContent("Hold ⌥ Option to show log settings on builds.");
#else
            public static readonly GUIContent altToBuildLogs =
                EditorGUIUtility.TrTextContent("Hold Alt to show log settings on builds.");
#endif

#if UNITY_EDITOR_OSX
            public static readonly GUIContent showInExplorer = EditorGUIUtility.TrTextContent("Reveal in Finder");
#else
            public static readonly GUIContent showInExplorer = EditorGUIUtility.TrTextContent("Show in Explorer");
#endif
        }

        private void RepaintWindow()
        {
            if (!projectSettingsWindow)
            {
                return;
            }

            projectSettingsWindow.Repaint();
        }

        private void OnEnable()
        {
            // make sure ProjectSettings are editable
            ProjectSettings.instance.hideFlags &= ~HideFlags.NotEditable;

            var t = Type.GetType("UnityEditor.SettingsWindow,UnityEditor.dll");
            if (t != null)
            {
                var windows = Resources.FindObjectsOfTypeAll(t);
                if (windows.Length > 0)
                {
                    projectSettingsWindow = Resources.FindObjectsOfTypeAll(t)[0] as EditorWindow;
                }
            }

            EditorApplication.modifierKeysChanged += RepaintWindow;
            EditorApplication.projectChanged += OnProjectChanged;

            worldUDPPort = serializedObject.FindProperty("worldUDPPort");
            worldWebPort = serializedObject.FindProperty("worldWebPort");
            roomsUDPPort = serializedObject.FindProperty("roomsUDPPort");
            roomsWebPort = serializedObject.FindProperty("roomsWebPort");
            sendFrequency = serializedObject.FindProperty("sendFrequency");
            recvFrequency = serializedObject.FindProperty("recvFrequency");
            localRoomsCleanupTimeSeconds = serializedObject.FindProperty("localRoomsCleanupTimeSeconds");
            rsConsoleLogLevel = serializedObject.FindProperty(nameof(ProjectSettings.rsConsoleLogLevel));
            rsLogToFile = serializedObject.FindProperty(nameof(ProjectSettings.rsLogToFile));
            rsLogFilePath = serializedObject.FindProperty(nameof(ProjectSettings.rsLogFilePath));
            rsFileLogLevel = serializedObject.FindProperty(nameof(ProjectSettings.rsFileLogLevel));
            useToolkit = serializedObject.FindProperty("useToolkit");
            useCustomSchemas = serializedObject.FindProperty("useCustomSchemas");
            useCoherenceSyncSchema = serializedObject.FindProperty("useCoherenceSyncSchema");
            keepConnectionAlive = serializedObject.FindProperty("keepConnectionAlive");
            reportAnalytics = serializedObject.FindProperty("reportAnalytics");
            showHubSectionInfo = serializedObject.FindProperty(nameof(ProjectSettings.showHubSectionInfo));
            showHubModuleDescription = serializedObject.FindProperty(nameof(ProjectSettings.showHubModuleDescription));
            showHubMessageAreas = serializedObject.FindProperty(nameof(ProjectSettings.showHubMessageAreas));

            Refresh();

            mode = (Mode)SessionState.GetInt(modeSessionKey, 0);
            skipLongUnitTests = DefinesManager.IsSkipLongTestsDefineEnabled();
        }

        private void OnDisable()
        {
            EditorApplication.modifierKeysChanged -= RepaintWindow;
            EditorApplication.projectChanged -= OnProjectChanged;
            PortalLogin.StopPolling();
        }

        private void OnProjectChanged()
        {
            Refresh();
            Repaint();
        }

        private void Refresh()
        {
            ProjectSettings.instance.PruneSchemas();

            string[] guids = AssetDatabase.FindAssets("a:assets t:Coherence.SchemaAsset");
            schemas = new SchemaAsset[guids.Length];
            schemaContents = new GUIContent[guids.Length];
            schemaContext = new GUIContent[guids.Length];
            for (int i = 0; i < schemas.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileName(path);
                schemas[i] = AssetDatabase.LoadAssetAtPath<SchemaAsset>(path);
                schemaContents[i] = EditorGUIUtility.TrTextContent(fileName, path);
            }

            bakeFolder = ProjectSettings.instance.GetSchemaBakeFolderPath();
            schemaBakeFolderContent = EditorGUIUtility.TrTextContent(bakeFolder,
                "Path to the schema bake folder. You can move and rename this folder freely.");

            if (!string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.ProjectID))
            {
                Schemas.UpdateSyncState();
            }

            runtimeSettings = RuntimeSettings.Instance;

            if (runtimeSettingsSerializedObject != null)
            {
                runtimeSettingsSerializedObject.Dispose();
                runtimeSettingsSerializedObject = null;
            }

            if (runtimeSettings)
            {
                runtimeSettingsSerializedObject = new SerializedObject(runtimeSettings);
                localDevelopmentMode = runtimeSettingsSerializedObject.FindProperty("localDevelopmentMode");
                useNativeCore = runtimeSettingsSerializedObject.FindProperty("useNativeCore");
            }

            if (!string.IsNullOrEmpty(ProjectSettings.instance.LoginToken))
            {
                PortalLogin.FetchOrgs();
            }
        }

        public override void OnInspectorGUI()
        {
            ContentUtils.DrawCloneModeMessage();

            EditorGUI.BeginDisabledGroup(CloneMode.Enabled && !CloneMode.AllowEdits);
            serializedObject.Update();

            advanced = Event.current.modifiers == EventModifiers.Alt;

            if (lastAdvanced != advanced)
            {
                GUIUtility.keyboardControl = 0;
                lastAdvanced = advanced;
            }

            DrawMiscSettings();

            EditorGUILayout.Space();

            DrawSchemas();

            EditorGUILayout.Space();

            _ = EditorGUILayout.BeginVertical(ContentUtils.GUIStyles.header);
            EditorGUILayout.LabelField(GUIContents.bakeHeader, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(GUIContents.bakeDescription, ContentUtils.GUIStyles.wrappedLabel);
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel++;
            SharedModuleSections.DrawAutoBakeSection();
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            LoggingGUI();

            ReplicationServerBundling();

            HelpText();

            EditorGUILayout.Space();

            ReplicationServerGUI();

            DrawMissingRuntimeSettings();

            if (advanced || ProjectSettings.instance.UseCustomTools || ProjectSettings.instance.UseCustomEndpoints)
            {
                DrawAdvancedSettings();
                DrawVersionInfo();
            }
            else
            {
                _ = EditorGUILayout.BeginVertical(ContentUtils.GUIStyles.header);
                EditorGUILayout.LabelField(GUIContents.versionInfo, EditorStyles.boldLabel);
                EditorGUILayout.EndVertical();

                DrawVersionInfo();
                EditorGUILayout.LabelField(GUIContents.altToAdvanced, ContentUtils.GUIStyles.miniLabelGrey);
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                ProjectSettings.instance.Save();
            }

            if (runtimeSettingsSerializedObject != null)
            {
                _ = runtimeSettingsSerializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void DrawAdvancedSettings()
        {
            // advanced
            EditorGUILayout.LabelField(GUIContents.advanced, EditorStyles.boldLabel);

            // native client core
            if (useNativeCore != null)
            {
                _ = EditorGUILayout.PropertyField(useNativeCore, GUIContents.useNativeCore);
            }

            // custom tools
            EditorGUI.BeginChangeCheck();
            var uct = EditorGUILayout.Toggle(GUIContents.useCustomTools, ProjectSettings.instance.UseCustomTools);
            if (EditorGUI.EndChangeCheck())
            {
                ProjectSettings.instance.UseCustomTools = uct;
                if (uct && string.IsNullOrEmpty(ProjectSettings.instance.CustomToolsPath))
                {
                    var p = Environment.GetEnvironmentVariable("GOPATH");
                    ProjectSettings.instance.CustomToolsPath =
                        p != null ? Path.Combine(p, "bin") : Paths.nativeToolsPath;
                }
            }

            EditorGUI.BeginDisabledGroup(!ProjectSettings.instance.UseCustomTools);
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var path = EditorGUILayout.TextField(GUIContents.customToolsPath, ProjectSettings.instance.CustomToolsPath);
            if (EditorGUI.EndChangeCheck())
            {
                ProjectSettings.instance.CustomToolsPath = path;
            }

            if (GUILayout.Button("Browse", EditorStyles.miniButton,GUILayout.Width(60)))
            {
                var folder = EditorUtility.OpenFolderPanel("Select Custom Tools Path", ProjectSettings.instance.CustomToolsPath, "");
                if (!string.IsNullOrEmpty(folder))
                {
                    ProjectSettings.instance.CustomToolsPath = folder;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(ProjectSettings.instance.CustomToolsPath))
            {
                if (!File.Exists(Path.Combine(ProjectSettings.instance.CustomToolsPath, Paths.replicationServerName)))
                {
                    EditorGUILayout.HelpBox(
                        $"'{ProjectSettings.instance.CustomToolsPath}' does not contain a binary called '{Paths.replicationServerName}'.",
                        MessageType.Warning);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            EditorLauncher.StartInTerminal =
                EditorGUILayout.Toggle("Use Terminal as RS Host", EditorLauncher.StartInTerminal);
            // custom endpoint toggle

            EditorGUI.BeginChangeCheck();
            var use = EditorGUILayout.Toggle(GUIContents.useCustomEndpoints,
                ProjectSettings.instance.UseCustomEndpoints);
            if (EditorGUI.EndChangeCheck())
            {
                ProjectSettings.instance.UseCustomEndpoints = use;
                if (use && string.IsNullOrEmpty(ProjectSettings.instance.CustomAPIDomain))
                {
                    ProjectSettings.instance.CustomAPIDomain = Endpoints.apiDomain;
                }
            }

            // custom endpoints

            EditorGUI.BeginDisabledGroup(!ProjectSettings.instance.UseCustomEndpoints);
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            var apiDomain =
                EditorGUILayout.TextField(GUIContents.customAPIDomain, ProjectSettings.instance.CustomAPIDomain);
            if (EditorGUI.EndChangeCheck())
            {
                if (apiDomain.StartsWith("https://"))
                {
                    apiDomain = apiDomain.Substring(8);
                    if (apiDomain.IndexOf("/") > -1)
                    {
                        apiDomain = apiDomain.Substring(0, apiDomain.IndexOf("/"));
                    }
                }

                ProjectSettings.instance.CustomAPIDomain = apiDomain;
            }

            if (GUILayout.Button("local", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                ProjectSettings.instance.CustomAPIDomain = "localhost";
            }

            if (GUILayout.Button("stage", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                ProjectSettings.instance.CustomAPIDomain = "api.stage.coherence.io";
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            // skipping long tests
            EditorGUI.BeginChangeCheck();
            skipLongUnitTests = EditorGUILayout.Toggle("Skip Long Unit Tests", skipLongUnitTests);
            if (EditorGUI.EndChangeCheck())
            {
                DefinesManager.ApplySkipLongUnitTestsDefine(skipLongUnitTests);
            }

            // doc generation

            if (Directory.Exists(Paths.docFxPath))
            {
                EditorGUILayout.LabelField("API Docs", EditorStyles.boldLabel);

                EditorGUILayout.HelpBox(
                    "Custom DocFX required. Build instructions on the repository.",
                    MessageType.Info);

                if (EditorGUILayout.LinkButton("Custom DocFX"))
                {
                    Application.OpenURL("https://github.com/frarees/docfx/tree/feat/search-improvements");
                }
                EditorGUILayout.Space();

                EditorGUILayout.HelpBox("Make sure your .csprojs are generated and up-to-date. Go to Preferences > External Tools, disable \"Player projects\" and hit \"Regenerate project files\". Then, execute the following steps in order.",
                    MessageType.Info);

                if (File.Exists("Assets/csc.rsp"))
                {
                    EditorGUILayout.HelpBox(
                        "csc.rsp can interfere with docs generation.",
                        MessageType.Warning);
                }


                if (GUILayout.Button($"Create {Paths.directoryBuildTargetsFile}"))
                {
                    if (!DocGenUtil.HasDirectoryBuildTargets ||
                        (DocGenUtil.HasDirectoryBuildTargets &&
                        EditorUtility.DisplayDialog(Paths.directoryBuildTargetsFile,
                            $"{Paths.directoryBuildTargetsFile} file exists. Override?",
                            "OK", "Cancel")))
                    {
                        DocGenUtil.GenerateDirectoryBuildTargets();
                    }
                }

                if (GUILayout.Button("Build XMLs"))
                {
                    DocGenUtil.RunBuildSolution();
                }

                if (GUILayout.Button("Build Metadata"))
                {
                    DocGenUtil.FetchBuildArtifacts();
                }

                if (GUILayout.Button("Build & Serve Site"))
                {
                    DocGenUtil.RunDocFx();
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Open DocFX Folder"))
                {
                    EditorUtility.RevealInFinder(Paths.docFxConfigPath);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Baking", EditorStyles.boldLabel);

            var bakeNow = BakeUtil.Outdated;
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Bake", EditorStyles.miniButton);
            if (GUILayout.Button(bakeNow ? GUIContents.bakeNow : GUIContents.bake,
                    bakeNow ? ContentUtils.GUIStyles.boldButton : EditorStyles.miniButton))
            {
                BakeUtil.Bake();
            }

            EditorGUILayout.EndHorizontal();

            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Files");
            EditorGUI.BeginDisabledGroup(!Directory.Exists(Paths.defaultSchemaBakePath));
            if (GUILayout.Button("Delete Bake Files", EditorStyles.miniButton))
            {
                CodeGenSelector.Clear(true);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button(GUIContents.advancedBakeText))
            {
                AdvancedBakeWizard.Open();
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

            if (GUILayout.Button("Select HubModuleManager"))
            {
                Selection.activeObject = HubModuleManager.instance;
            }

            if (GUILayout.Button("Log GitBook URLs"))
            {
                var urls = DocumentationLinks.ActiveKeys
                    .ToDictionary(key => key, DocumentationLinks.GetDocsUrl)
                    .Select(pair => $"{pair.Key}\n<a href=\"{pair.Value}\">{pair.Value}</a>");

                var documentedTypes = TypeCache
                    .GetTypesWithAttribute<HelpURLAttribute>()
                    .ToDictionary(type => type, type => type.GetCustomAttribute<HelpURLAttribute>().URL)
                    .Where(pair => pair.Value.Contains("//docs.coherence.io/"));

                var componentUrls = documentedTypes.Select(pair => $"{pair.Key}\n<a href=\"{pair.Value}\">{pair.Value}</a>");

                Debug.Log("GitBook URLs\n" + string.Join("\n", urls) + "\n\nComponent URLs\n" + string.Join("\n", componentUrls));
            }
        }

        private void DrawMiscSettings()
        {
            if (runtimeSettings)
            {
                _ = EditorGUILayout.PropertyField(localDevelopmentMode, GUIContents.localDevelopmentMode);
            }

            _ = EditorGUILayout.PropertyField(keepConnectionAlive, GUIContents.keepConnectionAliveLabel);
            _ = EditorGUILayout.PropertyField(reportAnalytics, GUIContents.reportAnalytics);
        }

        private void DrawVersionInfo()
        {
            var vi = ProjectSettings.instance.RuntimeSettings.VersionInfo;
            var hasVi = vi != null;
            ContentUtils.DrawSelectableLabel("SDK", hasVi ? vi.Sdk : "Unknown",
                ContentUtils.GUIStyles.miniLabelGreyWrap);
            ContentUtils.DrawSelectableLabel("Engine", hasVi ? vi.Engine : "Unknown",
                ContentUtils.GUIStyles.miniLabelGreyWrap);
        }

        private void DrawScriptingDefine(string define)
        {
            _ = EditorGUILayout.BeginHorizontal();
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] defines);

            var hasDefine = Array.IndexOf(defines, define) != -1;
            var content = EditorGUIUtility.IconContent(hasDefine ? "Toolbar Minus" : "Toolbar Plus");
            if (GUILayout.Button(content, ContentUtils.GUIStyles.iconButton, GUILayout.ExpandWidth(false)))
            {
                if (hasDefine)
                {
                    ArrayUtility.Remove(ref defines, define);
                }
                else
                {
                    ArrayUtility.Add(ref defines, define);
                }

                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
            }

            ContentUtils.DrawSelectableLabel(define, ContentUtils.GUIStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void HelpText()
        {
            _ = EditorGUILayout.BeginVertical(ContentUtils.GUIStyles.header);
            EditorGUILayout.LabelField(GUIContents.helpTextHeader, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(GUIContents.helpTextDescription, ContentUtils.GUIStyles.wrappedLabel);
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel++;
            _ = EditorGUILayout.PropertyField(showHubSectionInfo, GUIContents.showHubHelpInfo);
            _ = EditorGUILayout.PropertyField(showHubModuleDescription, GUIContents.showHubModuleDescription);
            _ = EditorGUILayout.PropertyField(showHubMessageAreas, GUIContents.showHubMessages);
            EditorGUI.indentLevel--;
        }

        private void LoggingGUI()
        {
            if (!runtimeSettings)
            {
                return;
            }

            _ = EditorGUILayout.BeginVertical(ContentUtils.GUIStyles.header);
            EditorGUILayout.LabelField(GUIContents.logsHeader, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            var logSettings = Log.GetSettings();

            EditorGUI.indentLevel++;
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    logSettings.LogLevel =
                        (LogLevel)EditorGUILayout.EnumPopup(GUIContents.consoleLogLevel, logSettings.LogLevel);

                    DefinesManager.ApplyCorrectLogLevelDefines(logSettings.LogLevel);

                    EditorGUI.indentLevel++;
                    _ = EditorGUILayout.BeginHorizontal();
                    {
                        logSettings.SourceFilters = EditorGUILayout.TextField(logSettings.SourceFilters);
                        var indent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;
                        // enum props are always rendererd honoring indent, but here we want to have an "inline" element in horizontal space
                        logSettings.FilterMode =
                            (Log.FilterMode)EditorGUILayout.EnumPopup(logSettings.FilterMode, GUILayout.Width(68));
                        EditorGUI.indentLevel = indent;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;

                    logSettings.EditorLogLevel =
                        (LogLevel)EditorGUILayout.EnumPopup(GUIContents.editorLogLevel, logSettings.EditorLogLevel);

                    logSettings.LogStackTrace = EditorGUILayout.Toggle(GUIContents.logStackTrace, logSettings.LogStackTrace);

                    if (advanced || logSettings.LogToFile)
                    {
                        logSettings.LogToFile = EditorGUILayout.Toggle(GUIContents.logToFile, logSettings.LogToFile);

                        if (logSettings.LogToFile)
                        {
                            logSettings.LogFilePath =
                                EditorGUILayout.TextField(GUIContents.logFilePath, logSettings.LogFilePath);
                            logSettings.FileLogLevel =
                                (LogLevel)EditorGUILayout.EnumPopup(GUIContents.fileLogLevel, logSettings.FileLogLevel);
                        }
                    }

                    if (change.changed)
                    {
                        logSettings.Save();
                    }
                }

                if (advanced)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(
                        "Builds default log level is Info. Use the following defines to modify this behaviour for the current build target:",
                        ContentUtils.GUIStyles.miniLabelGreyWrap);
                    DrawScriptingDefine(LogConditionals.DisableInfo);
                    DrawScriptingDefine(LogConditionals.DisableWarning);
                    DrawScriptingDefine(LogConditionals.DisableError);
                }
                else
                {
                    EditorGUILayout.LabelField(GUIContents.altToBuildLogs, ContentUtils.GUIStyles.miniLabelGreyWrap);
                }
            }
            EditorGUI.indentLevel--;
        }

        private void ReplicationServerBundling()
        {
            if (!runtimeSettings)
            {
                return;
            }

            using var change = new EditorGUI.ChangeCheckScope();

            var projectSettings = ProjectSettings.instance;

            var enableBundling = EditorGUILayout.Toggle(GUIContents.replicationServerBundlingHeader,
                projectSettings.RSBundlingEnabled);

            if (change.changed)
            {
                projectSettings.RSBundlingEnabled = enableBundling;
                projectSettings.Save();
            }
        }

        private void ReplicationServerGUI()
        {
            _ = EditorGUILayout.BeginVertical(ContentUtils.GUIStyles.header);
            EditorGUILayout.LabelField(GUIContents.replicationServer, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            _ = EditorGUILayout.BeginHorizontal();
            var r = EditorGUILayout.BeginVertical(ContentUtils.GUIStyles.frameBox);

            EditorGUI.indentLevel++;
            var modeCount = 2;
            for (int i = 0; i < modeCount; i++)
            {
                var tabRect = ContentUtils.GetTabRect(r, i, modeCount, out GUIStyle tabStyle);
                EditorGUI.BeginChangeCheck();
                var m = GUI.Toggle(tabRect, (int)mode == i, ((Mode)i).ToString(), tabStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    GUI.FocusControl(null);
                    if (m)
                    {
                        mode = (Mode)i;
                        SessionState.SetInt(modeSessionKey, i);
                    }
                }
            }

            _ = GUILayoutUtility.GetRect(10, 22);

            // inner frame contents

            if (mode == Mode.Rooms)
            {
                EditorGUI.BeginChangeCheck();
                _ = EditorGUILayout.PropertyField(roomsUDPPort, GUIContents.port);
                _ = EditorGUILayout.PropertyField(roomsWebPort, GUIContents.webPort);
                _ = EditorGUILayout.PropertyField(sendFrequency);
                _ = EditorGUILayout.PropertyField(recvFrequency);
                _ = EditorGUILayout.PropertyField(localRoomsCleanupTimeSeconds);
                if (EditorGUI.EndChangeCheck())
                {
                    roomsUDPPort.intValue = roomsUDPPort.intValue < 0 ? 0 : roomsUDPPort.intValue;
                    if (runtimeSettings)
                    {
                        roomsWebPort.intValue = roomsWebPort.intValue < 0 ? 0 : roomsWebPort.intValue;
                    }

                    sendFrequency.intValue = sendFrequency.intValue < 1 ? 1 : sendFrequency.intValue;
                    recvFrequency.intValue = recvFrequency.intValue < 1 ? 1 : recvFrequency.intValue;
                }
            }
            else if (mode == Mode.Worlds)
            {
                EditorGUI.BeginChangeCheck();
                _ = EditorGUILayout.PropertyField(worldUDPPort, GUIContents.port);
                _ = EditorGUILayout.PropertyField(worldWebPort, GUIContents.webPort);
                _ = EditorGUILayout.PropertyField(sendFrequency);
                _ = EditorGUILayout.PropertyField(recvFrequency);
                if (EditorGUI.EndChangeCheck())
                {
                    worldUDPPort.intValue = worldUDPPort.intValue < 0 ? 0 : worldUDPPort.intValue;
                    worldWebPort.intValue = worldWebPort.intValue < 0 ? 0 : worldWebPort.intValue;
                    sendFrequency.intValue = sendFrequency.intValue < 1 ? 1 : sendFrequency.intValue;
                    recvFrequency.intValue = recvFrequency.intValue < 1 ? 1 : recvFrequency.intValue;
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (advanced)
            {
                EditorGUILayout.LabelField(
                    "Enable features by pressing the '+' button. Use the following defines to modify this behaviour for the current build target:",
                    ContentUtils.GUIStyles.miniLabelGreyWrap);
                DrawScriptingDefine(FeatureFlags.BackupWorldData);
                EditorGUILayout.Space();
            }

            if (advanced || rsLogToFile.boolValue)
            {
                EditorGUILayout.PropertyField(rsConsoleLogLevel);
                EditorGUILayout.PropertyField(rsLogToFile);
                if (rsLogToFile.boolValue)
                {
                    EditorGUILayout.PropertyField(rsLogFilePath);
                    EditorGUILayout.PropertyField(rsFileLogLevel);
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawMissingRuntimeSettings()
        {
            if (!runtimeSettings)
            {
                if (GUILayout.Button("Initialize Runtime Settings", EditorStyles.miniButton))
                {
                    Postprocessor.UpdateRuntimeSettings();
                    Refresh();
                }
            }
        }

        private void DrawUploadSchemasSettings()
        {
            SharedModuleSections.DrawAutoUploadSection();
        }

        private void DrawCustomSchemas()
        {
            for (int i = 0; i < schemas.Length; i++)
            {
                var schema = schemas[i];
                if (AssetDatabase.GetAssetPath(schema) == Paths.gatherSchemaPath)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    _ = EditorGUILayout.Toggle(schemaContents[i], schema, GUILayout.ExpandWidth(false));
                    EditorGUI.EndDisabledGroup();
                    return;
                }

                EditorGUI.BeginDisabledGroup(!schema);
                _ = EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                bool active = EditorGUILayout.Toggle(schemaContents[i], ProjectSettings.instance.HasSchema(schemas[i]),
                    GUILayout.ExpandWidth(false));
                if (EditorGUI.EndChangeCheck())
                {
                    if (active)
                    {
                        ProjectSettings.instance.AddSchema(schemas[i]);
                    }
                    else
                    {
                        ProjectSettings.instance.RemoveSchema(schemas[i]);
                    }

                    Schemas.UpdateSyncState();
                }

                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                if (schemaContext[i] != null)
                {
                    EditorGUILayout.LabelField(schemaContext[i]);
                }
                EditorGUI.indentLevel = indent;
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawSchemas()
        {
            _ = EditorGUILayout.BeginVertical(ContentUtils.GUIStyles.header);
            EditorGUILayout.LabelField(GUIContents.schemasHeader, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(GUIContents.schemasDescription, ContentUtils.GUIStyles.wrappedLabel);
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            // NOTE use property when we allow disabling useToolkit
            // _ = EditorGUILayout.PropertyField(useToolkit, GUIContents.coherenceSyncSupportLabel);
            EditorGUI.BeginDisabledGroup(true);
            _ = EditorGUILayout.Toggle(GUIContents.coherenceSyncSupportLabel, ProjectSettings.instance.UseToolkit);
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck())
            {
                Schemas.UpdateSyncState();
            }

            EditorGUI.BeginChangeCheck();
            _ = EditorGUILayout.PropertyField(useCoherenceSyncSchema, GUIContents.gatheredSchemasLabel);
            if (EditorGUI.EndChangeCheck())
            {
                Schemas.UpdateSyncState();
            }

            EditorGUI.BeginChangeCheck();
            _ = EditorGUILayout.PropertyField(useCustomSchemas, GUIContents.useCustomSchemasLabel);
            if (EditorGUI.EndChangeCheck())
            {
                if (useCustomSchemas.boolValue)
                {
                    Refresh();
                    Repaint();
                }

                Schemas.UpdateSyncState();
            }

            if (ProjectSettings.instance.UseCustomSchemas)
            {
                EditorGUI.indentLevel++;
                DrawCustomSchemas();
                EditorGUI.indentLevel--;
            }

            DrawUploadSchemasSettings();

            EditorGUI.indentLevel--;
        }
    }
}
