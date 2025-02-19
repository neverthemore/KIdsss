// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Editor.Portal;
    using Coherence.Editor.Toolkit;
    using Coherence.Toolkit;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using Common;

    internal static class StatusTrackerConstructor
    {
        private const string PrefID = "coherence.issuecard.init";

        public static class StatusTrackerIDs
        {
            public const string HasServerRunningRoomWizardID = "tracker.HasRunningRoom";
            public const string HasServerRunningWorldWizardID = "tracker.HasRunningWorld";
            public const string HasRoomUpToDateWizardID = "tracker.HasRoomUpToDate";
            public const string HasWorldUpToDateWizardID = "tracker.HasWorldUpToDate";

            public const string BridgeWizardID = "tracker.HasBridgeWizard";
            public const string LiveQueryWizardID = "tracker.HasQueryWizard";

            public const string EnterPlayModeOptionsID = "tracker.EnterPlayModeOptions";

            public const string MapperGUIDIssuesWizardID = "tracker.HasMapperGUIDIssues";
            public const string AllSyncsMappedWizardID = "tracker.AllSyncsMapped";
            public const string SchemaChangedWizardID = "tracker.SchemaOutOfDate";
            public const string LoggedInToPortalWizardID = "tracker.LoggedInToPortal";
            public const string PortalCredentialsSetWizardID = "tracker.PortalCredentialsSet";
            public const string SchemaInSyncWizardId = "tracker.SchemaInSync";
            public const string VersionUpToDate = "tracker.VersionUpToDate";
            public const string VersionNotSupported = "tracker.VersionNotSupported";
            public const string CoherenceSyncAdded = "tracker.CoherenceSyncAdded";
            public const string CoherenceInputAdded = "tracker.CoherenceInputAdded";
            public const string SampleUIAdded = "tracker.SampleUIAdded";
            public const string RunInBackgroundID = "tracker.RunInBackground";
        }

        public static class Scopes
        {
            public const string ReplicationServer = "scope.rs";
            public const string AccountStatus = "scope.accountstatus";
            public const string ProjectStatus = "scope.projectstatus";
        }

        public static void InitTrackers()
        {
            StatusTrackerController.instance.ClearTrackers();

            StatusTrackerController.instance.Add(new TrackerIssue(StatusTrackerIDs.RunInBackgroundID, Scopes.ProjectStatus, StatusTracker.Severity.Message,
                    new IssueConditionCustom(() => { return Application.runInBackground; }),
                    new IssueSolutionAction(() =>
                    {
                        Application.runInBackground = true;
                    }),
                    new GUIContent($"'Run In Background' is disabled "),
                    new GUIContent($"When testing locally with multiple clients, it is highly recommended to turn on 'Run in Background' in Project Settings -> Player -> Resolution and Presentation"),
                    new GUIContent($"Enable"),
                    true,
                    null,
                    StatusTrackerController.UpdateHandler.ProjectChanged, StatusTrackerController.UpdateHandler.SelectionChanged
                ));

            #region Scene
            StatusTrackerController.instance.Add(new TrackerIssue(StatusTrackerIDs.BridgeWizardID, Scopes.ProjectStatus, StatusTracker.Severity.Message,
#if UNITY_2023_1_OR_NEWER
                    new IssueConditionCustom(() => Object.FindAnyObjectByType<CoherenceBridge>() != null),
#else
                    new IssueConditionCustom(() => { return MonoBehaviour.FindObjectOfType<CoherenceBridge>() != null; }),
#endif
                    new IssueSolutionAction(() =>
                    {
                        Utils.AddBridgeInstanceInScene(null);
                    }),
                    new GUIContent($"Add CoherenceBridge to scene"),
                    new GUIContent($"CoherenceBridge is an interface between the GameObject and coherence. See documentation for more information"),
                    new GUIContent($"Add CoherenceBridge"),
                    false,
                    () => DocumentationLinks.GetDocsUrl(DocumentationKeys.AddBridge), //Issue with not being avaliable here. Maybe use ID in a dict where we store links
                    StatusTrackerController.UpdateHandler.HierarchyChanged
                ));

            StatusTrackerController.instance.Add(new TrackerIssue(StatusTrackerIDs.LiveQueryWizardID, Scopes.ProjectStatus, StatusTracker.Severity.Message,
#if UNITY_2023_1_OR_NEWER
                    new IssueConditionCustom(() => Object.FindAnyObjectByType<CoherenceLiveQuery>() != null),
#else
                    new IssueConditionCustom(() => { return MonoBehaviour.FindObjectOfType<CoherenceLiveQuery>() != null; }),
#endif

                    new IssueSolutionAction(() =>
                    {
                        Utils.AddLiveQueryInstanceInScene(null);
                    }),
                    new GUIContent($"Add LiveQuery to scene"),
                    new GUIContent($"Use LiveQuery to define a limited area of interest for an optimized gaming experience. See documentation for more information"),
                    new GUIContent($"Add LiveQuery"),
                    true,
                    () => DocumentationLinks.GetDocsUrl(DocumentationKeys.AddLiveQuery),
                    StatusTrackerController.UpdateHandler.HierarchyChanged
                ));
            #endregion

            #region Schemas
            StatusTrackerController.instance.Add(new TrackerIssue(StatusTrackerIDs.SchemaChangedWizardID, Scopes.ProjectStatus,
                StatusTracker.Severity.Warning,
                new IssueConditionCustom(() => !BakeUtil.Outdated),
                new IssueSolutionAction(() => BakeUtil.Bake()),
                new GUIContent("Schema Outdated"),
                new GUIContent(
                    "In order to use cloudbased replication servers, you need to upload an up to date schema"),
                new GUIContent("Bake"),
                true,
                () => DocumentationLinks.GetDocsUrl(DocumentationKeys.Schemas),
                StatusTrackerController.UpdateHandler.OnAssetsChanged
            ));
            #endregion

            #region Cloud
            StatusTrackerController.instance.Add(new TrackerIssue(StatusTrackerIDs.VersionUpToDate, Scopes.AccountStatus, StatusTracker.Severity.Message,
                new IssueConditionCustom(() => CoherenceHub.info.IsLatestVersion),
                new IssueSolutionAction(() => UnityEditor.PackageManager.UI.Window.Open(Paths.packageId)),
                OverviewModule.ModuleGUIContents.VersionUpdateAvaliable,
                new GUIContent("New coherence SDK version available."),
                new GUIContent("Update"),
                true,
                null,
                StatusTrackerController.UpdateHandler.OnEnable
                ));

            StatusTrackerController.instance.Add(new TrackerIssue(StatusTrackerIDs.LoggedInToPortalWizardID, Scopes.AccountStatus, StatusTracker.Severity.Message,
                new IssueConditionCustom(() => PortalLogin.IsLoggedIn),
                new IssueSolutionAction(() => PortalLogin.Login(null)),
                new GUIContent("Not logged in to coherence Cloud"),
                new GUIContent("Log in or sign up to get access to online features"),
                new GUIContent("Login"),
                true,
                () => DocumentationLinks.GetDocsUrl(DocumentationKeys.DeveloperPortalOverview),
                StatusTrackerController.UpdateHandler.OnLoginStatusChange
                ));

            ///Use TrackerDashboardIssue instead if we want login flow directly in overview
            StatusTrackerController.instance.Add(new TrackerDashboardIssue(StatusTrackerIDs.PortalCredentialsSetWizardID, Scopes.AccountStatus, StatusTracker.Severity.Message,
                new GUIContent("Select an organization and a project","Select an organization and a project from the coherence Cloud tab. If there aren't any available, go to the coherence Portal and set them up first")
                ));

            StatusTrackerController.instance.Add(new TrackerIssue(StatusTrackerIDs.SchemaInSyncWizardId, Scopes.AccountStatus, StatusTracker.Severity.Message,
                new IssueConditionCustom(() => (!PortalLogin.IsLoggedIn &&
                                                !string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.ProjectID)) ||
                                               Schemas.state != Schemas.SyncState.OutOfSync),
                new IssueSolutionAction(() =>
                {
                    Schemas.UploadActive(InteractionMode.UserAction);
                }),
                new GUIContent("Cloud Schema out of Sync"),
                new GUIContent("In order to use cloudbased replication servers, you need to upload an up to date schema"),
                new GUIContent("Upload schema"),
                true,
                () => DocumentationLinks.GetDocsUrl(DocumentationKeys.UploadSchema),
                StatusTrackerController.UpdateHandler.OnBakeEnded
            ));
            #endregion
        }
    }
}
