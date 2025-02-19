// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

using Coherence.Editor.Portal;
using Coherence.Editor.Toolkit;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Coherence.Editor
{
    internal static class SharedModuleSections
    {
        internal static class GUIContents
        {
            public static readonly GUIContent syncStatus = EditorGUIUtility.TrTextContent("Cloud Status");

            public static readonly GUIContent bake = EditorGUIUtility.TrTextContent("Bake Now", "Create network code based on active schemas.");
            public static readonly GUIContent bakeNow = EditorGUIUtility.TrTextContentWithIcon("Bake Now", "Your network code for your active schemas is outdated, bake now to fix it.", "Warning");

            public static readonly GUIContent uploadSettings = EditorGUIUtility.TrTextContent("Upload Automatically");

            public static readonly GUIContent gatherBeforeBake = EditorGUIUtility.TrTextContent("Gather On Bake", "When baking, make sure the CoherenceSync schema is gathered.");
            public static readonly GUIContent bakeAutomatically = EditorGUIUtility.TrTextContent("Bake Automatically");

            public static readonly GUIContent localSchemaId = new GUIContent("Local Schema ID");
            public static readonly GUIContent openDashboardWorlds = new GUIContent("Worlds Dashboard");
        }

        internal static void DrawSchemasInPortal()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var text = BakeUtil.HasSchemaID ? BakeUtil.SchemaID.Substring(0, 5) : "No Schema";
                var content = new GUIContent(text);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(GUIContents.localSchemaId, content);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var content = PortalUtil.OrgAndProjectIsSet ? Schemas.StateContent : new GUIContent();
                CoherenceHubLayout.DrawLabel(GUIContents.syncStatus, content, options:GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();

                CoherenceHubLayout.DrawCloudDependantButton(CoherenceHubLayout.GUIContents.refresh, Schemas.UpdateSyncState, string.Empty);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawCloudDependantButton(OnlineModule.ModuleGUIContents.upload, () =>
                {
                    Schemas.UploadActive(InteractionMode.UserAction);
                }, "You need to login to sync schemas.",
                () => !PortalUtil.OrgAndProjectIsSet);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space();
                DrawDashboardWorldsButton();
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("When using worlds, if you upload a schema and want to start using it, you need to edit the world to point to that schema. You can do this from the dashboard.", UnityEditor.MessageType.Info);
        }

        internal static string GetDashboardUrl(string organizationSlug)
        {
            var org = organizationSlug ?? string.Empty;
            var url = $"{ExternalLinks.PortalUrl}/{PortalUrlMangler(org)}";
            return url;
        }

        internal static string GetOrganizationUsageUrl(string organizationSlug)
            => $"{GetDashboardUrl(organizationSlug)}/usage";

        internal static string GetOrganizationBillingUrl(string organizationSlug)
            => $"{GetDashboardUrl(organizationSlug)}/billing";

        internal static string GetDashboardWorldsUrl(string projectName, string organizationSlug)
        {
            var proj = projectName ?? string.Empty;
            var org = organizationSlug ?? string.Empty;
            string url = projectName == PortalLoginDrawer.NoneProjectName ?
                Endpoints.portalUrl :
                $"{ExternalLinks.PortalUrl}/{PortalUrlMangler(org)}/{PortalUrlMangler(proj)}/worlds";

            return url;
        }

        internal static string GetDashboardProjectUrl(string projectName, string organizationName)
        {
            var proj = projectName ?? string.Empty;
            var org = organizationName ?? string.Empty;
            string url = projectName == PortalLoginDrawer.NoneProjectName ?
                Endpoints.portalUrl :
                $"{ExternalLinks.PortalUrl}/{PortalUrlMangler(org)}/{PortalUrlMangler(proj)}";

            return url;
        }

        private static string PortalUrlMangler(string url)
        {
            return url.ToLower().Replace(" ", "-");
        }

        internal static void DrawDashboardWorldsButton()
        {
            var url = GetDashboardWorldsUrl(PortalLoginDrawer.GetSelectedProjectName(), PortalLoginDrawer.GetSelectedOrganization()?.slug);

            var content = GUIContents.openDashboardWorlds;
            content.tooltip = url;
            CoherenceHubLayout.DrawLink(content, url);
        }

        internal static void DrawBakeAction(bool expand = true)
        {
            var bakeNow = BakeUtil.Outdated;
            if (GUILayout.Button(bakeNow ? GUIContents.bakeNow : GUIContents.bake, EditorStyles.miniButton, GUILayout.ExpandWidth(expand)))
            {
                BakeUtil.Bake();
                GUIUtility.ExitGUI();
            }
        }

        internal static void DrawAutoUploadSection()
        {
            EditorGUILayout.LabelField(GUIContents.uploadSettings);

            EditorGUI.indentLevel++;

            PortalUtil.UploadOnEnterPlayMode = EditorGUILayout.Toggle("On Enter Play Mode", PortalUtil.UploadOnEnterPlayMode);
            PortalUtil.UploadOnBuild = EditorGUILayout.Toggle("On Unity Player Build", PortalUtil.UploadOnBuild);
            PortalUtil.UploadAfterBake = EditorGUILayout.Toggle("On Baking Complete", PortalUtil.UploadAfterBake);

            EditorGUI.indentLevel--;
        }

        internal static void DrawAutoBakeSection()
        {

            if (ProjectSettings.instance.UseCoherenceSyncSchema)
            {
                BakeUtil.GatherOnBake = EditorGUILayout.Toggle(GUIContents.gatherBeforeBake, BakeUtil.GatherOnBake);
            }

            EditorGUILayout.LabelField(GUIContents.bakeAutomatically);

            EditorGUI.indentLevel++;

            BakeUtil.BakeOnEnterPlayMode = EditorGUILayout.Toggle("On Enter Play Mode", BakeUtil.BakeOnEnterPlayMode);
            BakeUtil.BakeOnBuild = EditorGUILayout.Toggle("On Unity Player Build", BakeUtil.BakeOnBuild);

            EditorGUI.indentLevel--;
        }
    }
}
