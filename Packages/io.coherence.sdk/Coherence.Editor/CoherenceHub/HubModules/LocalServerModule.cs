// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using Coherence.Toolkit.ReplicationServer;
    using Features;
    using Portal;
    using ReplicationServer;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;

    [Serializable, HubModule(Priority = 30)]
    public class LocalServerModule : HubModule
    {
        public override string ModuleName => "Replication Servers";

        public static class ModuleGUIContents
        {
            public static readonly GUIContent RsDescription = new GUIContent(
                "All clients and simulators connects to the Replication Server, a smart relay and an entity database with multiple optimization features. Configurable in settings.");

            public static readonly GUIContent MrsButton = new GUIContent("Configure Multi-Room Simulators");

            public static readonly GUIContent MrsDescription = new GUIContent(
                "Multi-Room Simulators are Room Simulators which are able to simulate multiple game rooms at the same time. If you want to run Simulators in the Editor (without a separate build) or run multiple Simulators in the same build, configure MRS.");

            public static readonly GUIContent TerminalRooms = Icons.GetContentWithText("Coherence.Terminal",
                "Run for Rooms", "Start a terminal with a Replication Server handling rooms.");

            public static readonly GUIContent Terminal = Icons.GetContentWithText("Coherence.Terminal",
                "Run for Worlds", "Start a terminal with a Replication Server handling worlds.");

            public static readonly GUIContent HeadlessBuildExecutable = Icons.GetContentWithText("Coherence.Terminal",
                string.Empty, "Generate a executable file to perform and upload the build with Unity in batch mode.");

            public static readonly GUIContent Clipboard =
                Icons.GetContent("Coherence.RunCommand", "Copy the run command to the clipboard.");

            public static readonly GUIContent Copied =
                Icons.GetContentWithText("Coherence.RunCommand", "Run command copied.");

            public static readonly GUIContent ReplicationServer = new GUIContent("Local Replication Server");

            public static readonly GUIContent WhatAreRPs = new GUIContent(
                "In order for clients to communicate with each other, they need a replication server. A replication server can either run locally or in the cloud. The responsibility of the server is to replicate the state of the world across the network." +
                "\nIf a new schema has been created, you also need to restart the replication server.");

            public static readonly GUIContent RevealInFinder = new GUIContent(
#if UNITY_EDITOR_WIN
                "Show Storage in Explorer"
#elif UNITY_EDITOR_OSX
                "Reveal Storage in Finder"
#else
                "Open Storage Folder"
#endif
            );

            public static readonly GUIContent UseWorldPersistence = new GUIContent("Enable",
                "Enables serialization of persistent data into a storage file (JSON), which is loaded back when a World Replication Server is restarted.");

            public static readonly GUIContent PersistenceStoragePath = new GUIContent("Storage",
                "File on disk where the Replication Server persistent entities are stored.");

            public static readonly GUIContent PersistenceStorageSaveRate = new GUIContent("Save Rate (seconds)",
                "How often should the replication server serialize the world information into the storage.");

            public static readonly GUIContent PersistenceStorageBackup =
                new GUIContent("Create Backup Now", "Create a backup of the world persistence storage.");

            public static readonly GUIContent PersistenceResetToDefaults = new GUIContent("Reset to Defaults",
                "Reset all persistence-related settings to their default values.");
        }

        public override HelpSection Help => new HelpSection
        {
            title = new GUIContent("What are Replication Servers?"),
            content = ModuleGUIContents.WhatAreRPs
        };

        public override void OnModuleEnable()
        {
            base.OnModuleEnable();
            Init();
        }

        private void Init()
        {
            EditorApplication.projectChanged += OnProjectChanged;
            Refresh();
        }

        public override void OnModuleDisable()
        {
            base.OnModuleDisable();
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void Refresh()
        {
            if (!string.IsNullOrEmpty(ProjectSettings.instance.LoginToken))
            {
                PortalLogin.FetchOrgs();
            }
        }

        private void OnProjectChanged()
        {
            Refresh();
        }

        public override void OnBespokeGUI()
        {
            CoherenceHubLayout.DrawSection(ModuleGUIContents.ReplicationServer, DrawReplicationServers);
            var backupWorldDataFeature = FeaturesManager.GetFeature(FeatureFlags.BackupWorldData);
            if (backupWorldDataFeature.IsEnabled || backupWorldDataFeature.IsUserConfigurable)
            {
                CoherenceHubLayout.DrawSection("Backup World Data", DrawSettingsForWorlds);
            }

            CoherenceHubLayout.DrawSection("Multi-room Simulators", DrawMrs);
        }

        private void DrawSettingsForWorlds()
        {
            var backupWorldDataFeature = FeaturesManager.GetFeature(FeatureFlags.BackupWorldData);
            if (backupWorldDataFeature.IsUserConfigurable)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.HelpBox(
                        "This feature has been deprecated. Disabling it is a permanent action.",
                        MessageType.Warning);
                    if (GUILayout.Button("Disable Feature"))
                    {
                        PersistenceUtils.UseWorldPersistence = false;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            var usePersistence = EditorGUILayout.Toggle(ModuleGUIContents.UseWorldPersistence,
                PersistenceUtils.UseWorldPersistence);
            if (EditorGUI.EndChangeCheck())
            {
                PersistenceUtils.UseWorldPersistence = usePersistence;
            }

            using (new EditorGUI.DisabledGroupScope(!usePersistence))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    CoherenceHubLayout.DrawDiskPath(
                        PersistenceUtils.StoragePath,
                        ModuleGUIContents.PersistenceStoragePath.tooltip,
                        () =>
                        {
                            var filePath = PersistenceUtils.StoragePath;
                            var defaultPath = File.Exists(filePath) ? filePath : Paths.libraryRootPath;
                            return EditorUtility.SaveFilePanel("Persistence Storage", defaultPath,
                                Paths.defaultPersistentStorageFileName + "." + Paths.persistentStorageFileExtension,
                                Paths.persistentStorageFileExtension);
                        },
                        path => PersistenceUtils.StoragePath = path);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    var rate = EditorGUILayout.IntField(ModuleGUIContents.PersistenceStorageSaveRate,
                        PersistenceUtils.SaveRateInSeconds);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PersistenceUtils.SaveRateInSeconds = Mathf.Max(rate, 1);
                    }
                }

                using (new EditorGUI.DisabledScope(!PersistenceUtils.CanBackup))
                using (new EditorGUILayout.HorizontalScope())
                {
                    var r = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false,
                        EditorGUIUtility.singleLineHeight, EditorStyles.miniButton, GUILayout.ExpandWidth(true)));
                    if (GUI.Button(r, ModuleGUIContents.PersistenceStorageBackup, EditorStyles.miniButton))
                    {
                        if (Host is EditorWindow)
                        {
                            var success = PersistenceUtils.Backup(out var backupPath);
                            MessageQueue.AddToQueue(success
                                ? new GUIContent($"Created {Path.GetFileName(backupPath)}")
                                : new GUIContent("Failed to backup"));
                        }
                    }
                }

                using (new EditorGUI.DisabledGroupScope(!File.Exists(PersistenceUtils.StoragePath)))
                {
                    var revealInFinderRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false,
                        EditorGUIUtility.singleLineHeight, EditorStyles.miniButton, GUILayout.ExpandWidth(true)));
                    if (GUI.Button(revealInFinderRect, ModuleGUIContents.RevealInFinder, EditorStyles.miniButton))
                    {
                        EditorUtility.RevealInFinder(PersistenceUtils.StoragePath);
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    var r = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false,
                        EditorGUIUtility.singleLineHeight, EditorStyles.miniButton, GUILayout.ExpandWidth(true)));
                    if (GUI.Button(r, new GUIContent(ModuleGUIContents.PersistenceResetToDefaults),
                            EditorStyles.miniButton))
                    {
                        PersistenceUtils.StoragePath = null;
                        PersistenceUtils.SaveRateInSeconds = Constants.defaultPersistenceSaveRateInSeconds;
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                        GUIUtility.ExitGUI();
                    }
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("After changing any setting, please remember to restart your replication server.",
                MessageType.Info);
        }

        private void DrawReplicationServers()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawInfoLabel(ModuleGUIContents.RsDescription);
                CoherenceHubLayout.DrawActionLabel(GUIContents.Settings,
                    () => SettingsService.OpenProjectSettings(Paths.projectSettingsWindowPath));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ModuleGUIContents.TerminalRooms, EditorStyles.miniButtonLeft))
                {
                    EditorLauncher.RunRoomsReplicationServerInTerminal();
                }

                if (GUILayout.Button(ModuleGUIContents.Clipboard, EditorStyles.miniButtonRight, GUILayout.Width(22)))
                {
                    var command = Launcher.ToCommand(EditorLauncher.CreateLocalRoomsConfig());
                    GUIUtility.systemCopyBuffer = command;
                    MessageQueue.AddToQueue(ModuleGUIContents.Copied);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ModuleGUIContents.Terminal, EditorStyles.miniButtonLeft))
                {
                    EditorLauncher.RunWorldsReplicationServerInTerminal();
                }

                if (GUILayout.Button(ModuleGUIContents.Clipboard, EditorStyles.miniButtonRight, GUILayout.Width(22)))
                {
                    var command = Launcher.ToCommand(EditorLauncher.CreateLocalWorldConfig());
                    GUIUtility.systemCopyBuffer = command;
                    MessageQueue.AddToQueue(ModuleGUIContents.Copied);
                }
            }
        }

        public void DrawMrs()
        {
            CoherenceHubLayout.DrawInfoLabel(ModuleGUIContents.MrsDescription);
            if (CoherenceHubLayout.DrawButton(ModuleGUIContents.MrsButton))
            {
                CoherenceMainMenu.OpenMrsWizard();
            }
        }
    }
}
