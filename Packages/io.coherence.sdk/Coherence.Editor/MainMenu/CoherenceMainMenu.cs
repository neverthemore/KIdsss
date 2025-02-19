// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using Portal;
    using ReplicationServer;
    using Toolkit;
    using UI;
    using UnityEditor;
    using UnityEngine;

    internal static class CoherenceMainMenu
    {
        // Order of sections. Any separation of more than 10 creates a separator
        private const int Section1 = 1;
        private const int Section2 = 100;
        private const int Section3 = 200;
        private const int Section4 = 300;
        private const int Section5 = 400;

        public const string BackupWorldDataMenuItem = "coherence/Local Replication Server/Backup World Data";

        [MenuItem("coherence/Welcome", false, Section1)]
        internal static void OpenWelcomeWindow()
        {
            WelcomeWindow.Open();
        }

        [MenuItem("coherence/coherence Hub", false, Section1 + 1)]
        public static void OpenCoherenceHub()
        {
            CoherenceHub.Open();
        }

        [MenuItem("coherence/CoherenceSync Objects", false, Section1 + 2)]
        public static void OpenNetworkObjects()
        {
            CoherenceSyncObjectsStandaloneWindow.Open();
        }

        [MenuItem("coherence/Online Dashboard", false, Section1 + 3)]
        public static void DeveloperPortal()
        {
            UsefulLinks.DeveloperPortal();
        }


        [MenuItem("coherence/Explore Samples", false, Section1 + 4)]
        internal static void ShowAddDialogWindow(MenuCommand menuCommand)
        {
            SampleDialogPickerWindow.ShowWindow(parentGameObject: menuCommand.context as GameObject);
        }

        // Settings
        [MenuItem("coherence/Settings", false, Section2)]
        public static void OpenProjectSettings()
        {
            _ = SettingsService.OpenProjectSettings(Paths.projectSettingsWindowPath);
        }

        // Scene Setup
        [MenuItem("coherence/Scene Setup/Create CoherenceBridge", false, Section2 + 1)]
        internal static void AddBridge(MenuCommand menuCommand)
        {
            Utils.AddBridgeInstanceInScene(menuCommand);
        }

        [MenuItem("coherence/Scene Setup/Create LiveQuery", false, Section2 + 2)]
        internal static void AddLiveQuery(MenuCommand menuCommand)
        {
            Utils.AddLiveQueryInstanceInScene(menuCommand);
        }

        // GameObject Setup
        [MenuItem("coherence/GameObject Setup/Add CoherenceSync", true, Section2 + 4)]
        public static bool CanAddCoherenceSync()
        {
            return GameObjectSetup.ObjectHasNoSync();
        }

        [MenuItem("coherence/GameObject Setup/Add CoherenceSync", false, Section2 + 4)]
        public static void AddCoherenceSync()
        {
            GameObjectSetup.AddCoherenceSync();
        }

        [MenuItem("coherence/GameObject Setup/Configure", false, Section2 + 5)]
        public static void OpenSelectWindow()
        {
            _ = CoherenceSyncBindingsWindow.GetWindow();
        }

        [MenuItem("coherence/GameObject Setup/Optimize", false, Section2 + 6)]
        public static void OpenBindingsWindow()
        {
            BindingsWindow.Init();
        }

        // Servers
        [MenuItem("coherence/Local Replication Server/Run for Rooms %#&r", false, Section3)]
        public static void RunRoomsReplicationServerInTerminal()
        {
            EditorLauncher.RunRoomsReplicationServerInTerminal();
        }

        [MenuItem("coherence/Local Replication Server/Run for Worlds %#&w", false, Section3 + 1)]
        public static void RunWorldsReplicationServerInTerminal()
        {
            EditorLauncher.RunWorldsReplicationServerInTerminal();
        }

        [MenuItem(BackupWorldDataMenuItem, true, Section3 + 2)]
        private static bool BackupWorldDataValidate()
        {
            return !CloneMode.Enabled;
        }

#if COHERENCE_ENABLE_BACKUP_WORLD_DATA
        [MenuItem(BackupWorldDataMenuItem, false, Section3 + 2)]
#endif
        public static void BackupWorldData()
        {
            PersistenceUtils.UseWorldPersistence = !PersistenceUtils.UseWorldPersistence;
        }

        [MenuItem("coherence/Local Replication Server/Open in coherence Hub", false, Section3 + 3)]
        public static void OpenReplicationServerWindow()
        {
            CoherenceHub.Open();
            CoherenceHub.FocusModule<LocalServerModule>();
        }

        // Baking and schemas
        [MenuItem("coherence/Bake %#&m", true, Section3 + 1)]
        private static bool BakeValidate()
        {
            return !CloneMode.Enabled;
        }

        [MenuItem("coherence/Bake %#&m", false, Section3 + 1)]
        public static void BakeSchemas()
        {
            BakeUtil.Bake();
        }

        [MenuItem("coherence/Upload Schemas", false, Section3 + 2)]
        public static void UploadSchemas()
        {
            _ = Schemas.UploadActive(InteractionMode.UserAction);
        }

        [MenuItem("coherence/Upload Schemas", true, Section3 + 2)]
        public static bool UploadSchemasValidate()
        {
            return !string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.ProjectID);
        }

        // Game
        [MenuItem("coherence/Share/Build Upload", false, Section3 + 3)]
        public static void BuildGameWizard()
        {
            GameBuildWindow.Init();
        }

        // Simulators
        [MenuItem("coherence/Simulator/Multi-Room Simulator Wizard", false, Section3 + 4)]
        public static void OpenMrsWizard()
        {
            MultiRoomSimulatorsWizardModuleWindow.OpenWindow(true);
        }

        [MenuItem("coherence/Simulator/Simulator Build Wizard", false, Section3 + 5)]
        public static void OpenSimulatorWindow()
        {
            SimulatorWindow.Init();
        }

        [MenuItem("coherence/Simulator/Run Simulator Locally", true, Section3 + 6)]
        public static bool CanRunLocalSimulator()
        {
            return SimulatorEditorUtility.CanRunLocalSimulator();
        }

        [MenuItem("coherence/Simulator/Run Simulator Locally", false, Section3 + 7)]
        public static void RunLocalSimulator()
        {
            SimulatorEditorUtility.RunLocalSimulator();
        }

        [MenuItem("coherence/Simulator/Add AutoSimulatorConnection", false, Section3 + 7)]
        public static void AddAutoSimulatorConnection(MenuCommand menuCommand)
        {
            Utils.AddAutoSimulatorConnection(null);
        }


        [MenuItem("Assets/Migrate coherence Assets", true, 40), MenuItem("coherence/Migrate coherence Assets", true, Section4),]
        private static bool MenuMigrationValidate()
        {
            return !CloneMode.Enabled;
        }

        [MenuItem("Assets/Migrate coherence Assets", false, 40), MenuItem("coherence/Migrate coherence Assets", false, Section4),]
        public static void MenuMigration()
        {
            _ = Migration.Migrate();
        }

        [MenuItem("coherence/Update Bindings", true, Section4 + 1)]
        private static bool UpdateBindingsValidate()
        {
            return !CloneMode.Enabled;
        }

        [MenuItem("coherence/Update Bindings", false, Section4 + 1)]
        public static void UpdateBindings()
        {
            EditorCache.UpdateBindingsAndNotify();
        }

        // Links
        [MenuItem("coherence/Documentation", false, Section5)]
        public static void OpenDocumentation()
        {
            UsefulLinks.Documentation();
        }

        [MenuItem("coherence/Help/Community Forums", false, Section5 + 1)]
        public static void OpenCommunityForums()
        {
            UsefulLinks.CommunityForum();
        }

        [MenuItem("coherence/Help/Discord", false, Section5 + 2)]
        public static void OpenDiscord()
        {
            UsefulLinks.Discord();
        }

        [MenuItem("coherence/Help/Support", false, Section5 + 2)]
        public static void OpenSupport()
        {
            UsefulLinks.Support();
        }

        [MenuItem("coherence/Help/Report a Bug...", false, Section5 + 3)]
        private static void ReportABug() => BugReportHelper.DisplayReportBugDialogs();
    }
}
