// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEngine;
    using UnityEditor;
    using Coherence.Toolkit;
    using UnityEditor.SceneManagement;

    internal class BindingsWindowToolbar
    {
        private BindingsWindow editingWindow;

        private int toolbarHeight;
        private int footerHeight;
        internal BindingsWindowTreeFilters Filters { private set; get; }
        private GUIStyle darkVerticalStyle;

        internal BindingsWindowToolbar(BindingsWindow syncEditingWindow)
        {
            editingWindow = syncEditingWindow;
            toolbarHeight = BindingsWindowSettings.ToolbarHeight;
            footerHeight = BindingsWindowSettings.FooterHieght;
            Filters = new BindingsWindowTreeFilters(syncEditingWindow);

            darkVerticalStyle = UIHelpers.BackgroundStyle.Get("WindowBackground");
        }

        internal void DrawToolbar()
        {
            CoherenceSync sync = editingWindow.Component;
            _ = EditorGUILayout.BeginVertical(darkVerticalStyle);
            _ = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(toolbarHeight));

            DrawSync(sync);
            
            GUILayout.FlexibleSpace();

            Filters.DrawFilters();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        internal void DrawFooter() {
            _ = EditorGUILayout.BeginHorizontal(GUILayout.Height(footerHeight));
            GUILayout.Space(5);
            if (editingWindow.StateController.Lods)
            {
                BindingsWindowSettings.DrawSettings(editingWindow);
            }
            GUILayout.FlexibleSpace();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSync(CoherenceSync sync) {
            if (sync)
            {
                bool inStage = StageUtility.GetCurrentStage() != StageUtility.GetMainStage();
                bool isAsset = PrefabUtility.IsPartOfPrefabAsset(sync) || inStage;
                GUIStyle titleButton = new GUIStyle(EditorStyles.toolbarButton);
                titleButton.alignment = TextAnchor.MiddleLeft;
        
                var icon = isAsset ? AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(sync)) : PrefabUtility.GetIconForGameObject(sync.gameObject);
                var content = EditorGUIUtility.TrTextContentWithIcon(sync.name, icon);
                var width = titleButton.CalcSize(new GUIContent(sync.name)).x + 20f;
                if (GUILayout.Button(content, titleButton, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(width)))
                {
                    Selection.activeObject = sync.gameObject;
                    if (AssetDatabase.Contains(sync.gameObject))
                    {
                        EditorGUIUtility.PingObject(sync.gameObject);
                    }
                }
            }
        }

        private void DrawWarning(float width, float height)
        {
            GUILayout.Label(GUIContent.none, EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height));
            var rect = GUILayoutUtility.GetLastRect();

            GUI.Label(new Rect(rect.x + 3, rect.y, 24, rect.height), EditorGUIUtility.IconContent("d_console.warnicon.sml"));
            GUI.Label(new Rect(rect.x + 24, rect.y, rect.width - 27, rect.height), "Reducing the payload weights\n require a baked schema", EditorStyles.miniLabel);
        }

        private void DrawNeedBakeWarning(CoherenceSync sync)
        {
            if (!sync.usingReflection && ProjectSettings.instance.ActiveSchemasChanged)
            {
                EditorGUILayout.Space();
                GUILayout.Label(EditorGUIUtility.TrTextContentWithIcon("Please, bake again to apply changes to the baked scripts.", Icons.GetPath("Coherence.Bake.Warning")), ContentUtils.GUIStyles.centeredStretchedLabel);
                EditorGUILayout.Space();
            }
        }
    }
}
