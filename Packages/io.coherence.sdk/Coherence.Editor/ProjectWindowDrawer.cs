namespace Coherence.Editor
{
    using Portal;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    internal static class ProjectWindowDrawer
    {
        private static class GUIContents
        {
            public static readonly GUIContent Clone = EditorGUIUtility.TrIconContent(
            Icons.GetPath("Coherence.Clone"),
            "This Editor instance is a Clone. Clones don't trigger asset automations such as auto-updating prefabs, and features such as baking are disabled.");

            public static readonly GUIContent BakeOudated = EditorGUIUtility.TrIconContent(
                Icons.GetPath("Coherence.Bake.Warning"),
                "Bake to generate the scripts needed for networking.");

            public static readonly GUIContent CloudOutOfSync = EditorGUIUtility.TrIconContent(
                "console.warnicon",
                "Your local schema hasn't been uploaded to your current project.\n\nClick to upload.");

            public static readonly GUIContent NotLoggedIn = EditorGUIUtility.TrIconContent(
                Icons.GetPath("Logo.Icon.Disabled"),
                "You are not logged in.\n\nClick to open coherence Cloud window.");

            public static readonly GUIContent NoOrganizationSelected = EditorGUIUtility.TrIconContent(
            "console.warnicon",
                "No organization selected.\n\nClick to open coherence Cloud window.");

            public static readonly GUIContent NoProjectSelected = EditorGUIUtility.TrIconContent(
            "console.warnicon",
                "No project selected.\n\nClick to open coherence Cloud window.");

            public static readonly GUIContent StatusLoggedIn = EditorGUIUtility.TrIconContent(
                Icons.GetPath("Logo.Icon"),
                "Logged in to coherence Cloud.");
        }

        private static string coherenceFolderGuid;

        static ProjectWindowDrawer()
        {
            EditorApplication.projectWindowItemOnGUI += OnItemGUI;
            EditorApplication.projectChanged += OnProjectChanged;
            UpdateFolderGuid();
        }

        private static void OnProjectChanged() => UpdateFolderGuid();
        private static void UpdateFolderGuid() => coherenceFolderGuid = AssetDatabase.AssetPathToGUID(Paths.projectAssetsPath);

        private static void OnItemGUI(string guid, Rect rect)
        {
            if (guid != coherenceFolderGuid)
            {
                return;
            }

            // only render at smallest height
            var smallestHeight = 16f;
            if (!Mathf.Approximately(rect.height, smallestHeight))
            {
                return;
            }

            // precalculated size needed to render a folder with the name "coherence"
            var usedWidth = 80f;
            var iconWidth = 16f;
            if (rect.width <= usedWidth + iconWidth)
            {
                return;
            }

            var iconRect = rect;
            iconRect.xMin = iconRect.xMax - iconWidth;

            if (CloneMode.Enabled)
            {
                DrawIconButton(iconRect, GUIContents.Clone, true);
                return;
            }

            if(BakeUtil.Outdated)
            {
                if(DrawIconButton(iconRect,  GUIContents.BakeOudated))
                {
                    BakeUtil.Bake();
                }
                return;
            }

            if(!PortalLogin.IsLoggedIn)
            {
                if(DrawIconButton(iconRect, GUIContents.NotLoggedIn))
                {
                    OnlineModuleWindow.OpenWindow(true);
                }
                return;
            }

            if (OrganizationNotSet())
            {
                if (DrawIconButton(iconRect, GUIContents.NoOrganizationSelected))
                {
                    OnlineModuleWindow.OpenWindow(true);
                }

                return;
            }

            if (ProjectNotSet())
            {
                if (DrawIconButton(iconRect, GUIContents.NoProjectSelected))
                {
                    OnlineModuleWindow.OpenWindow(true);
                }

                return;
            }

            if (PortalUtil.SyncState is Schemas.SyncState.OutOfSync)
            {
                if (DrawIconButton(iconRect, GUIContents.CloudOutOfSync))
                {
                    Schemas.UploadActive(InteractionMode.UserAction);
                }

                return;
            }

            GUIContents.StatusLoggedIn.tooltip = GetValidStatusTooltip();

            if(DrawIconButton(iconRect, GUIContents.StatusLoggedIn))
            {
                CoherenceHub.Open();
            }
        }

        private static bool OrganizationNotSet() => string.IsNullOrEmpty(RuntimeSettings.Instance.OrganizationID);

        private static bool ProjectNotSet() => string.IsNullOrEmpty(RuntimeSettings.Instance.ProjectID);

        private static bool DrawIconButton(Rect rect, GUIContent content, bool disabled = false)
        {
            EditorGUI.BeginDisabledGroup(disabled);

            var style = disabled ? GUIStyle.none : ContentUtils.GUIStyles.iconButton;
            var clicked = GUI.Button(rect, content, style);

            EditorGUI.EndDisabledGroup();

            return clicked;
        }

        private static string GetValidStatusTooltip()
            => $"Logged in to coherence Cloud.\n\nProject: {RuntimeSettings.Instance.ProjectName}";
    }
}
