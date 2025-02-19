// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor {
	using System;
	using UnityEditor;
	using UnityEngine;
    using Coherence.Editor.Toolkit;

    internal class BindingsWindowSettings
    {

        internal static readonly Vector2 MinSize = new Vector2(350, 300);
        internal static readonly GUIContent Windowtitle = Icons.GetContentWithText("EditorWindow", "Optimization");
        internal static readonly GUIContent Hint = EditorGUIUtility.TrTextContent("Compress data to reduce bandwidth load of this entity. Use LODs to compress data more when the entity is further away");

        // Layout of Window bar
        internal const int HeaderHeight = 32; // 45
        internal const int ToolbarHeight = 20;
        internal const int TabsHeight = 25;
        internal const int LodFooterHeight = 45;

        internal const int LeftSideDefaultSize = 400;
        internal const int FooterHieght = 24;

        // Tree sections

        internal static BindingsWindowTreeHeader.WidthSetting LeftBarSettings = new BindingsWindowTreeHeader.WidthSetting(200, 180, 500);
        internal static BindingsWindowTreeHeader.WidthSetting LinkedBinginSettings = new BindingsWindowTreeHeader.WidthSetting(200, 100, 500);
        internal static BindingsWindowTreeHeader.WidthSetting BindingConfigSettings = new BindingsWindowTreeHeader.WidthSetting(220, 220, 300);
        internal static BindingsWindowTreeHeader.WidthSetting StatisticsSettings = new BindingsWindowTreeHeader.WidthSetting(150, 100, 400);
        internal static BindingsWindowTreeHeader.WidthSetting ValueRangeSettings = new BindingsWindowTreeHeader.WidthSetting(150, 100, 400);
        internal static BindingsWindowTreeHeader.WidthSetting TypeSettings = new BindingsWindowTreeHeader.WidthSetting(150, 100, 400);
        internal static BindingsWindowTreeHeader.WidthSetting SampleRateSettings = new BindingsWindowTreeHeader.WidthSetting(150, 100, 400);
        internal static BindingsWindowTreeHeader.WidthSetting LODSettings = new BindingsWindowTreeHeader.WidthSetting(175, 175, 300);

        internal static BindingsWindowTreeHeader.WidthSetting NewLodButton = new BindingsWindowTreeHeader.WidthSetting(120, 120, 120);

        // When clicking the warning button for 0 float values
        internal const float DefaultFloatResetValue = 1f;

        // Layout of LODs
        internal const float LODPrecisionFieldWidth = 65;
        internal const float LODBitDisplayWidth = 45;
        internal const float LODBitPercentageWidth = 26;
        internal const int LODBitDisplayCircleSize = 14;

        internal static float LODConstantWidth = LODPrecisionFieldWidth +LODBitDisplayWidth + (ShowBitPercentages ? LODBitPercentageWidth : 0);

        // MultiInput coloring
        internal static Color HighlightColor = new Color(0, .8f, .9f, 1);
        internal static Color WarningColor = new Color(1f, 0.76f, 0.03f);
        internal static Color StateHighlightColor = new Color(0.0f, .5f, .98f, 1f);

        // User settings
        internal static bool CanEditLODRanges = false;
        internal static bool ShowBitPercentages = true;
        internal static bool CompactView = true;
        internal static bool ShowStatistics = false;

        internal static Color HeaderColor => EditorGUIUtility.isProSkin ? DarkHeaderColor : LightHeaderColor;
        internal static Color RowColor => EditorGUIUtility.isProSkin ? DarkRowColor : LightRowColor;
        internal static Color RowSelectedColor => EditorGUIUtility.isProSkin ? DarkRowSelectedColor : LightRowSelectedColor;
        internal static Color ComponentRowColor => EditorGUIUtility.isProSkin ? DarkComponentRowColor : LightComponentRowColor;
        internal static Color HorizontalLineColor => EditorGUIUtility.isProSkin ? DarkHorizontalLineColor : LightHorizontalLineColor;
        internal static Color VerticalLineColor => EditorGUIUtility.isProSkin ? DarkVerticalLineColor : LightVerticalLineColor;

        // Row coloring
        private static Color LightRowColor = new Color(.83f, .83f, .83f, 1);
        private static Color LightRowSelectedColor = new Color(.89f, .89f, .89f, 1f);
        private static Color LightComponentRowColor = new Color(.69f, .69f, .69f, 1);
        private static Color LightHeaderColor = new Color(.65f, .65f, .65f, 1);

        private static Color LightHorizontalLineColor = new Color(0.80f, 0.80f, 0.80f, 1f);
        private static Color LightVerticalLineColor = new Color(0.5f, 0.5f, 0.5f, .4f);

        private static Color DarkRowColor = new Color(.22f, .22f, .22f, 1);
        private static Color DarkRowSelectedColor = new Color(.25f, .25f, .25f, 1f);
        private static Color DarkComponentRowColor = new Color(0.20f, 0.20f, 0.20f, 1);
        private static Color DarkHeaderColor = new Color(0.18f, 0.18f, 0.18f, 1);

        private static Color DarkHorizontalLineColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        private static Color DarkVerticalLineColor = new Color(0.5f, 0.5f, 0.5f, .4f);

        internal static void InitSettings()
        {
            CanEditLODRanges = EditorPrefs.GetBool("CoherenceSyncEditorUseAdvancedView", false);
            ShowBitPercentages = EditorPrefs.GetBool("CoherenceSyncEditorShowBitPercentages", false);
            CompactView = EditorPrefs.GetBool("CoherenceSyncEditorCompactView", false);
            ShowStatistics = EditorPrefs.GetBool("CoherenceSyncEditorShowStatistics", false);

        }


        internal static void DrawSettings(BindingsWindow editingWindow)
        {
            GUIStyle titleButton = new GUIStyle(EditorStyles.toolbarButton);
            titleButton.alignment = TextAnchor.MiddleLeft;

            EditorGUI.BeginChangeCheck();
            CanEditLODRanges = false;

            GUIContent percetageContent = new GUIContent("%", "Show Percentages");
            GUI.contentColor = ShowBitPercentages ? HighlightColor : Color.white;
            if (GUILayout.Button(percetageContent, EditorStyles.toolbarButton))
            {
                ShowBitPercentages = !ShowBitPercentages;
                EditorPrefs.SetBool("CoherenceSyncEditorShowBitPercentages", ShowBitPercentages);
            }

            GUIContent compactViewContent = new GUIContent("Compact", "Show Percentages");
            GUI.contentColor = CompactView ? HighlightColor : Color.white;
            if (GUILayout.Button(compactViewContent, EditorStyles.toolbarButton))
            {
                CompactView = !CompactView;
                EditorPrefs.SetBool("CoherenceSyncEditorCompactView", CompactView);
            }
            GUI.contentColor = Color.white;

            GUIContent statisticsContent = new GUIContent("Bandwidth", "Show bandwidth");
            GUI.contentColor = ShowStatistics ? HighlightColor : Color.white;
            if (GUILayout.Button(statisticsContent, EditorStyles.toolbarButton))
            {
                SetStatisticsActive(!ShowStatistics);
                editingWindow.TreeHeader.SetStatisticsVisible(ShowStatistics);
            }
            GUI.contentColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                editingWindow.RefreshTree(false);
            }
        }

        internal static void SetStatisticsActive(bool show)
        {
            ShowStatistics = show;
            EditorPrefs.SetBool("CoherenceSyncEditorShowStatistics", ShowStatistics);
        }
    }
}
