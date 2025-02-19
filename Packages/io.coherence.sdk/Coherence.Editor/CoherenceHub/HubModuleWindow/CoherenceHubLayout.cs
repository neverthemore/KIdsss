// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;

    //Our implementation of UnityEditor.EditorGUILayout
    internal static class CoherenceHubLayout
    {
        private const string skinName = "CoherenceHubSkin";
        private static GUISkin coherenceSkin;
        internal static float SectionHeaderTopSpacing = 8;
        internal static float SectionHeaderBottomSpacing = 6;
        internal static float SectionHorizontalSpacing = 4;
        internal static float SectionSpacing = 8;
        internal static float SectionBottomSpacing = 4;
        internal static float LabelBottomSpacing = 6;

        private static bool showLoginDetails;
        private static GenericPopup loginDetailsPopup;

        private static GUISkin CoherenceSkin
        {
            get
            {
                if (coherenceSkin == null)
                {
                    coherenceSkin = GetCurrentSkin();
                }

                return coherenceSkin;
            }
        }

        private static GUISkin GetCurrentSkin()
        {
            return AssetDatabase.LoadAssetAtPath<GUISkin>(GetSkinPath(EditorGUIUtility.isProSkin));
        }

        private static string GetSkinPath(bool isDark, string overrideName = null)
        {
            return $"{Paths.uiAssetsPath}/{((overrideName != null) ? overrideName : skinName)}{(isDark ? "_dark" : "_light")}.guiskin";
        }

        public static class GUIContents
        {
            public static readonly GUIContent title = Icons.GetContentWithText("EditorWindow", "Quickstart");
            public static readonly GUIContent description = EditorGUIUtility.TrTextContent("coherence is a network engine and a platform that allows you to easily make multiplayer experiences and host them in the cloud.");
            public static readonly GUIContent headToTheDocs = EditorGUIUtility.TrTextContent("View the online documentation.");

            public static readonly GUIContent discordLogo = new GUIContent(Icons.GetContent("Quickstart.Discord.Logo", "Opens a link to our Discord."));
            public static readonly GUIContent documentationIcon = new GUIContent(Icons.GetContent("Quickstart.Documentation.Icon", "Online documentation."));
            public static readonly GUIContent devPortal = new GUIContent(Icons.GetContent("Quickstart.DevPortal.Icon", "Open the developer portal."));

            public static readonly GUIContent discordText = new GUIContent("Join our Discord", "Opens a link to our Discord.");
            public static readonly GUIContent documentationText = new GUIContent("Documentation", "Online documentation.");
            public static readonly GUIContent devPortalText = new GUIContent("Developer Portal", "Open the developer portal.");
            public static readonly GUIContent correctlySetupHint = Icons.GetContentWithText("Coherence.IssueWizard.Passed", "", "Everything has been correctly setup");
            public static readonly GUIContent refresh = Icons.GetContent("Coherence.Sync", "Refresh");
        }

        public static class Styles
        {
            public static readonly GUIStyle Toolbar = CoherenceSkin.GetStyle("Toolbar");
            public static readonly GUIStyle HeaderToolbarButton = CoherenceSkin.GetStyle("HeaderToolbarButton");
            public static readonly GUIStyle ToolbarButton = CoherenceSkin.GetStyle("ToolbarButton");
            public static readonly GUIStyle ToolbarToggle = CoherenceSkin.GetStyle("toolbarbutton");
            public static readonly GUIStyle BlueButton = CoherenceSkin.GetStyle("BlueButton");
            public static readonly GUIStyle SmallBlueButton = CoherenceSkin.GetStyle("SmallBlueButton");
            public static readonly GUIStyle WarningDismissButton = CoherenceSkin.GetStyle("WarningDismissButton");
            public static readonly GUIStyle Button = new GUIStyle(GUI.skin.button)
            {
                wordWrap = true,
            };
            public static readonly GUIStyle ButtonNoLineWrap = new GUIStyle(GUI.skin.button)
            {
                wordWrap = false,
            };

            public static readonly GUIStyle LabelButtonWithPadding = new GUIStyle(EditorStyles.label)
            {
                padding = { left = 8, right = 8 }
            };

            public static readonly GUIStyle SmallLabel = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
            public static readonly GUIStyle InfoLabel = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            public static readonly GUIStyle Label = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                clipping = TextClipping.Overflow
            };

            public static readonly GUIStyle WhiteLabel = new GUIStyle(Label)
            {
                normal = { textColor = Color.white }
            };

            public static readonly GUIStyle BoldLabel = new GUIStyle(Label)
            {
                fontStyle = FontStyle.Bold,
            };

            public static readonly GUIStyle LargeLabel = new GUIStyle(Label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft
            };

            public static readonly GUIStyle WrappedTextField = new GUIStyle(EditorStyles.textField)
            {
                wordWrap = true,
            };

            public static readonly GUIStyle SectionHeader = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.LowerLeft,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                stretchHeight = true,
                richText = true,
                clipping = TextClipping.Overflow
            };

            public static readonly GUIStyle VersionHeader = new GUIStyle(EditorStyles.linkLabel)
            {
                fontStyle = FontStyle.Normal,
                normal = { textColor = GetCoherencePrimaryColor() }
            };

            public static readonly GUIStyle Header = CoherenceSkin.GetStyle("header");
            public static readonly GUIStyle WarningButton = CoherenceSkin.GetStyle("WarningButton");

            public static readonly GUIStyle Grid = new GUIStyle(GUI.skin.button)
            {
                stretchWidth = true
            };

            public static readonly GUIStyle linkStyle2020 = new GUIStyle(EditorStyles.linkLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Italic
            };

            public static readonly GUIStyle Bullet = new GUIStyle(Label)
            {
                richText = true,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };

            public static readonly GUIStyle HeaderBackgroundWithLogin = new GUIStyle()
            {
                padding = { bottom = 4, top = 4, left = 8, right = 20 },
                fixedHeight = 48f
            };

            public static readonly GUIStyle HeaderBackground = new GUIStyle()
            {
                padding = { bottom = 4, top = 4, left = 4, right = 24 },
                fixedHeight = 48f
            };

            public static readonly GUIStyle InspectorHeaderBackground = new GUIStyle()
            {
                fixedHeight = 28f
            };

            public static readonly GUIStyle WelcomeWindowContent = new GUIStyle()
            {
                padding = { bottom = 4, top = 4, left = 24, right = 24 }
            };

            public static readonly GUIStyle HeaderToolbar = new GUIStyle()
            {
                padding = { bottom = 4, top = 4, left = 8, right = 20 }
            };

            public static readonly GUIStyle SectionBox = new GUIStyle(EditorStyles.helpBox);

            public static readonly GUIStyle PopupNonFixedHeight = new GUIStyle(EditorStyles.popup)
            {
                fixedHeight = 0f
            };

            public static readonly GUIStyle HorizontalMargins = new GUIStyle()
            {
                margin = { bottom = 0, top = 0, left = 8, right = 8 }
            };
        }

        internal static bool DrawHelpFoldout(bool showing, HubModule.HelpSection help)
        {
            if (!ProjectSettings.instance.showHubModuleDescription)
            {
                return false;
            }

            bool bExpand = false;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bExpand = CustomFoldout(showing, help.title, Styles.InfoLabel);

                if (bExpand)
                {
                    using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                    {
                        EditorGUILayout.Space(SectionHorizontalSpacing);
                        DrawListLabel(help.content);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.Space(SectionHorizontalSpacing);
                    }
                }
            }
            return bExpand;
        }

        internal static bool CustomFoldout(bool value, GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            content.image = value ? EditorGUIUtility.IconContent("IN_foldout_on").image : EditorGUIUtility.IconContent("IN_foldout").image;

            if (GUILayout.Button(content, style, options))
            {
                return !value;
            }

            return value;
        }

        public static Color GetCoherencePrimaryColor()
        {
            ColorUtility.TryParseHtmlString("#29ABE2", out var color);
            return color;
        }

        public static void DrawIssuesList(string scopeId)
        {
            using EditorGUILayout.VerticalScope scope = new EditorGUILayout.VerticalScope();

            var activeIssues = StatusTrackerController.instance.GetActiveTrackersForScope(scopeId);
            foreach (var issue in activeIssues)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    issue.Draw();
                }
            }
        }

        internal static void DrawIssuesListForScope(string scope)
        {
            DrawIssuesList(scope);
        }

        internal static void DrawBulletPoint(GUIContent content, GUIContent buttonContent, Action action, int indent = 1)
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
            {
                DrawBulletPoint(content);
                GUILayout.FlexibleSpace();
                DrawActionLabel(buttonContent, action);
                GUILayout.Space(8);
            }
        }

        internal static void DrawBulletPoint(GUIContent content, int indent = 1)
        {
            for (int i = 0; i < indent; i++)
            {
                EditorGUI.indentLevel++;
            }

            var bulletContent = new GUIContent(content);
            bulletContent.text = "\u2022 " + bulletContent.text;
            var contentWidth = Styles.Bullet.CalcSize(bulletContent).x;
            EditorGUIUtility.labelWidth = contentWidth;
            EditorGUILayout.LabelField(bulletContent, Styles.Bullet);
            EditorGUIUtility.labelWidth = 0;

            for (int i = 0; i < indent; i++)
            {
                EditorGUI.indentLevel--;
            }
        }

        public static bool DrawBrowseButton()
        {
            return GUILayout.Button("Browse", EditorStyles.miniButton, GUILayout.Width(64));
        }

        public static void DrawDiskPath(string currentPath, string infoString, Func<string> unityPanel, Action<string> onChangePath)
        {
            _ = EditorGUILayout.BeginHorizontal(GUI.skin.box);
            var text = string.IsNullOrEmpty(currentPath) ? infoString : currentPath;
            EditorGUILayout.SelectableLabel(text, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button("Browse", EditorStyles.miniButton, GUILayout.Width(64)))
            {
                string path = unityPanel.Invoke();

                if (!string.IsNullOrEmpty(path))
                {
                    onChangePath?.Invoke(path);
                }

                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        public static float DrawSlider(string text, float value, float leftValue, float rightValue, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            if (TryDrawIssue(wizard, () => EditorGUILayout.Slider(text, value, leftValue, rightValue, options), ref value))
            {
                return value;
            }

            return EditorGUILayout.Slider(text, value, leftValue, rightValue, options);
        }

        public static bool DrawToggle(string text, bool value, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            if (TryDrawIssue(wizard, () => EditorGUILayout.Toggle(text, value, options), ref value))
            {
                return value;
            }

            return EditorGUILayout.Toggle(text, value, options);
        }

        public static string DrawTextField(string text, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            if (TryDrawIssue(wizard, () => EditorGUILayout.TextField(text, options), ref text))
            {
                return text;
            }

            return EditorGUILayout.TextField(text, options);
        }

        public static string DrawTextField(string label, string text, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            if (TryDrawIssue(wizard, () => EditorGUILayout.TextField(label, text, options), ref text))
            {
                return text;
            }

            return EditorGUILayout.TextField(label, text, options);
        }

        public static int DrawIntField(string label, int value, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            if (TryDrawIssue(wizard, () => EditorGUILayout.IntField(label, value, options), ref value))
            {
                return value;
            }

            return EditorGUILayout.IntField(label, value, options);
        }

        public static int DrawGrid(int tabindex, GUIContent[] contents, params GUILayoutOption[] options)
        {
            var longestTextWidth = contents.Length > 0 ?
                Styles.Grid.CalcSize(contents.OrderByDescending(x => x.text.Length).First()).x
                + Styles.Grid.margin.horizontal : 0;

            var columnCount = Mathf.FloorToInt(EditorGUIUtility.currentViewWidth / longestTextWidth);
            columnCount = Mathf.Clamp(columnCount, 1, contents.Length);
            var grid = Styles.Grid;
            return GUILayout.SelectionGrid(tabindex, contents, columnCount, grid, options);
        }

        public static void DrawLargeLabel(string text, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            if (!TryDrawIssue(wizard, () => EditorGUILayout.LabelField(text, Styles.LargeLabel, options)))
            {
                EditorGUILayout.LabelField(text, Styles.LargeLabel, options);
            }
        }

        public static void DrawBoldLabel(GUIContent content, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            if (!TryDrawIssue(wizard, () => EditorGUILayout.LabelField(content, Styles.BoldLabel, options)))
            {
                EditorGUILayout.LabelField(content, Styles.BoldLabel, options);
            }
        }

        internal static float GetWidthForBoldLabel(GUIContent content)
        {
            return Styles.BoldLabel.CalcSize(content).x;
        }

        public static void DrawListLabel(GUIContent content, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (!TryDrawIssue(wizard, () => EditorGUILayout.LabelField(content, Styles.Label, options)))
                {
                    EditorGUILayout.LabelField(content, Styles.Label, options);
                }
            }
        }

        public static void DrawListLabel(string text, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (!TryDrawIssue(wizard, () => EditorGUILayout.LabelField(text, Styles.Label, options)))
                {
                    EditorGUILayout.LabelField(text, Styles.Label, options);
                }
            }
        }

        public static void DrawLabel(GUIContent content, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            if (!TryDrawIssue(wizard, () => EditorGUILayout.LabelField(content, Styles.Label, options)))
            {
                EditorGUILayout.LabelField(content, Styles.Label, options);
            }
        }

        internal static void DrawLabel(GUIContent label, GUIContent content, TrackerIssue wizard = null, params GUILayoutOption[] options)
        {
            Action drawer = ()=> EditorGUILayout.LabelField(label, content, Styles.Label, options);

            if (!TryDrawIssue(wizard, drawer))
            {
                drawer.Invoke();
            }
        }

        internal static float GetWidthForLabel(GUIContent content)
        {
            return Styles.Label.CalcSize(content).x;
        }

        public static bool DrawClickableLabel(string text, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, Styles.Label);
        }

        public static bool DrawClickableLabelSmall(string text, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, Styles.VersionHeader);
        }

        public static void DrawMessageArea(GUIContent content)
        {
            if (!ProjectSettings.instance.showHubMessageAreas)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                DrawIconButton(EditorGUIUtility.IconContent("console.infoicon.sml"));
                EditorGUILayout.LabelField(content, Styles.InfoLabel);
            }
        }

        public static void DrawMessageArea(string text)
        {
            if (!ProjectSettings.instance.showHubMessageAreas)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                DrawIconButton(EditorGUIUtility.IconContent("console.infoicon.sml"));
                EditorGUILayout.LabelField(text, Styles.InfoLabel);
            }
        }

        public static void DrawWarnArea(GUIContent content, bool useBox = true)
        {
            if (useBox)
            {
                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    DrawIconButton(EditorGUIUtility.IconContent("console.warnicon.sml"));
                    EditorGUILayout.LabelField(content, Styles.InfoLabel);
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawIconButton(EditorGUIUtility.IconContent("console.warnicon.sml"));
                    EditorGUILayout.LabelField(content, Styles.InfoLabel);
                }
            }
        }

        public static bool DrawWarnArea(string text, bool useBox = true, bool dismissible = false)
        {
            var skin = useBox ? GUI.skin.box : GUIStyle.none;

            using (new EditorGUILayout.HorizontalScope(skin))
            {
                DrawIconButton(EditorGUIUtility.IconContent("console.warnicon.sml"));
                EditorGUILayout.LabelField(text, Styles.InfoLabel);

                if (dismissible)
                {

                    return GUILayout.Button(EditorGUIUtility.TrIconContent("d_winbtn_win_close"), Styles.WarningDismissButton);
                }
            }

            return false;
        }

        public static void DrawErrorArea(GUIContent content)
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                DrawIconButton(EditorGUIUtility.IconContent("console.erroricon.sml"));
                EditorGUILayout.LabelField(content, Styles.InfoLabel);
            }
        }

        public static void DrawErrorArea(string text)
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                DrawIconButton(EditorGUIUtility.IconContent("console.erroricon.sml")) ;
                EditorGUILayout.LabelField(text, Styles.InfoLabel);
            }
        }

        public static void DrawInfoAreaWithBulletPoints(string text, params string[] bulletPoints)
        {
            if (!ProjectSettings.instance.showHubSectionInfo)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawIconButton(EditorGUIUtility.IconContent("console.infoicon.sml"));
                    DrawLabel(new GUIContent(text));
                }

                foreach (var bulletPoint in bulletPoints)
                {
                    DrawBulletPoint(new GUIContent(bulletPoint), 2);
                }
            }
        }

        public static void DrawInfoLabel(GUIContent content)
        {
            if (!ProjectSettings.instance.showHubSectionInfo)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(content, Styles.InfoLabel);
                GUILayout.Space(LabelBottomSpacing);
            }
        }

        public static void DrawInfoLabel(string text)
        {
            if (!ProjectSettings.instance.showHubSectionInfo)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(text, Styles.InfoLabel);
                GUILayout.Space(LabelBottomSpacing);
            }
        }

        public static void DrawSmallInfoLabel(GUIContent content)
        {
            EditorGUILayout.LabelField(content, Styles.SmallLabel);
        }

        public static void DrawSmallInfoLabel(string text)
        {
            EditorGUILayout.LabelField(text, Styles.SmallLabel);
        }

        public static bool DrawIconButton(GUIContent content)
        {
            return GUILayout.Button(content, EditorStyles.label, GUILayout.ExpandWidth(false));
        }

        public static bool DrawButton(GUIContent content, bool allowWordWrap = true, params GUILayoutOption[] options)
        {
            return GUILayout.Button(content, allowWordWrap ? Styles.Button : Styles.ButtonNoLineWrap, options);
        }

        public static bool DrawButton(string text, bool allowWordWrap = true, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, allowWordWrap ? Styles.Button : Styles.ButtonNoLineWrap, options);
        }

        internal static bool DrawButtonBlue(GUIContent content, params GUILayoutOption[] options)
        {
            return GUILayout.Button(content, Styles.BlueButton, options);
        }

        internal static bool DrawButtonBlue(string text, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, Styles.BlueButton, options);
        }

        internal static bool DrawSmallButtonBlue(GUIContent content, params GUILayoutOption[] options)
        {
            return GUILayout.Button(content, Styles.SmallBlueButton, options);
        }

        internal static bool DrawSmallButtonBlue(string text, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, Styles.SmallBlueButton, options);
        }

        internal static void DrawCloudDependantButton(GUIContent content, Action onButtonPress, string tooltip, Func<bool> disableConditions = null, params GUILayoutOption[] options)
        {
            bool additionalDisableConditions = disableConditions?.Invoke() ?? false;

            var isPortalAvailable = PortalUtil.CanCommunicateWithPortal;
            GUIContent conditionalContent = null;

            if (!isPortalAvailable || !string.IsNullOrEmpty(tooltip))
            {
                conditionalContent = new GUIContent(content);
                if (conditionalContent.image==null)
                {
                    conditionalContent.image = isPortalAvailable ? null : TrackerIssue.WarningContextMessages.IssueWarningIcon.image;
                }
                conditionalContent.tooltip = tooltip;
            }


            using (new EditorGUI.DisabledScope(!PortalUtil.CanCommunicateWithPortal || additionalDisableConditions))
            {
                if (DrawButton(conditionalContent ?? content, true, options))
                {
                    onButtonPress.Invoke();
                    GUIUtility.ExitGUI();
                }
            }
        }

        public static bool DrawButtonWithPadding(GUIContent content, params GUILayoutOption[] options)
        {
            return GUILayout.Button(content, Styles.LabelButtonWithPadding, options);
        }

        public static bool DrawButtonAsPopup(Rect rect, string text)
        {
            return GUI.Button(rect, text, EditorStyles.popup);
        }

        public static bool DrawHeaderToolbarButton(string text, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, Styles.HeaderToolbarButton, options);
        }

        public static bool DrawToolbarButton(GUIContent content, params GUILayoutOption[] options)
        {
            return GUILayout.Button(content, Styles.ToolbarButton, options);
        }

        public static bool DrawToolbarButton(string text, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, Styles.ToolbarButton, options);
        }

        internal static void DrawPrefixLabel(GUIContent content, TrackerIssue wizard = null, GUIStyle followingStyle = null)
        {
            if (!TryDrawIssue(wizard, () => EditorGUILayout.PrefixLabel(content, EditorStyles.miniButton, EditorStyles.label)))
            {
                var followingStylePick = followingStyle != null ? followingStyle : EditorStyles.miniButton;
                EditorGUILayout.PrefixLabel(content, followingStylePick, EditorStyles.label);
            }
        }

        public static bool DrawToolbarToggle(bool value, string text)
        {
            return GUILayout.Toggle(value, text, Styles.ToolbarToggle);
        }

        public static bool DrawMiniButton(GUIContent content)
        {
            return GUILayout.Button(content, EditorStyles.miniButton, GUILayout.ExpandWidth(false));
        }

        public static void DrawActionLabel(GUIContent content, Action action)
        {
            if(EditorGUILayout.LinkButton(content))
            {
                action?.Invoke();
            }
        }

        public static void DrawLink(GUIContent content, string url)
        {
            DrawActionLabel(content, () => Application.OpenURL(url));
        }

        public static string DrawModuleFrame(HubModule module, string searchText, SearchField searchField, Action UICallback = null)
        {
            using (var hScope = new EditorGUILayout.HorizontalScope(Styles.HeaderToolbar))
            {
                EditorGUI.DrawRect(new Rect(hScope.rect) { height = 2.5f }, Color.black);
                ColorUtility.TryParseHtmlString("#1a1a1a", out var rectColor);
                EditorGUI.DrawRect(hScope.rect, rectColor);

                GUILayout.FlexibleSpace();

                if (searchField != null)
                {
                    searchText = searchField.OnToolbarGUI(searchText);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (module != null && HubModule.GUIContents.ModuleDocumentationLinks.TryGetValue(module.GetType(), out DocumentationKeys urlKey))
                    {
                        var url = DocumentationLinks.GetDocsUrl(urlKey);
                        DrawDocumentationLink(url, true, true);
                    }
                }

                if (DrawButtonWithPadding(HubModule.GUIContents.SettingsIcon))
                {
                    SettingsService.OpenProjectSettings(Paths.projectSettingsWindowPath);
                }

                if (module != null && module.AllowsUndocking && DrawButtonWithPadding(HubModule.GUIContents.UnDock))
                {
                    module.Dock(!module.IsDocked);
                }

                UICallback?.Invoke();
            }

            return searchText;
        }

        public static void DrawDocumentationLink(string url, bool addPadding, bool forceLightIcon = false)
        {
            if ((addPadding) ?
                DrawButtonWithPadding(forceLightIcon ? HubModule.GUIContents.HelpLight : HubModule.GUIContents.Help) :
                GUILayout.Button(forceLightIcon ? HubModule.GUIContents.HelpLight : HubModule.GUIContents.Help, EditorStyles.label, GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL(url);
            }
        }

        public static void DrawSection(string title, Action sectionContent)
        {
            GUILayout.Space(SectionSpacing);
            using (var vScope = new EditorGUILayout.VerticalScope(Styles.SectionBox))
            {
                DrawSectionHeader(title);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(SectionHorizontalSpacing);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        sectionContent?.Invoke();
                    }
                    GUILayout.Space(SectionHorizontalSpacing);
                }
                GUILayout.Space(SectionBottomSpacing);
            }
        }

        public static void DrawSection(GUIContent content, Action sectionContent, Action UICallback = null, float? customSectionSpacing = null)
        {
            var sectionSpacing = customSectionSpacing.HasValue ? customSectionSpacing.Value : SectionSpacing;

            GUILayout.Space(sectionSpacing);
            using (new EditorGUILayout.VerticalScope(Styles.SectionBox))
            {
                DrawSectionHeader(content, UICallback);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(SectionHorizontalSpacing);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        sectionContent?.Invoke();
                    }
                    GUILayout.Space(SectionHorizontalSpacing);
                }
                GUILayout.Space(SectionBottomSpacing);
            }
        }

        internal static void DrawValidatedSection(GUIContent title, Func<bool> validator, Action notValidateDrawer, Action validatedDrawer)
        {
            if (!validator.Invoke())
            {
                DrawSection(title, () =>
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        notValidateDrawer?.Invoke();
                    };
                });
            }
            else
            {
                DrawSection(title, validatedDrawer, () => DrawIconButton(GUIContents.correctlySetupHint));
            }
        }

        internal static void DrawDismissableSection(string prefID, GUIContent header, GUIContent content)
        {
            DrawDismissableSection(prefID, header, () => DrawLabel(content));
        }

        internal static void DrawDismissableSection(string prefID, GUIContent header, Action UICallback)
        {
            if (EditorPrefs.GetBool(prefID, true))
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    DrawSectionHeader(header);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(SectionHorizontalSpacing);

                        using (new EditorGUILayout.VerticalScope())
                        {
                            UICallback?.Invoke();

                            GUILayout.Space(LabelBottomSpacing);
                            if (DrawButton("Dismiss"))
                            {
                                EditorPrefs.SetBool(prefID, false);
                            }
                            GUILayout.Space(SectionHorizontalSpacing);
                            GUILayout.Space(SectionBottomSpacing);
                        }

                        GUILayout.Space(SectionHorizontalSpacing);
                    }
                }
            }
        }

        public static int DrawToolbar(int selected, GUIContent[] contents)
        {
            return GUILayout.Toolbar(selected, contents);
        }

        public static void DrawHubModule(HubModule other)
        {
            other.DrawModuleGUI();
        }

        private static bool TryDrawIssue<T>(TrackerIssue issue, Func<T> returnVal, ref T outVal)
        {
            bool? succes = issue?.GetLastEvaluation();
            if (succes.HasValue && succes.Value == false)
            {
                using (var hScope = new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                {
                    GUIContent contentWithContext = new GUIContent(StatusTracker.WarningContextMessages.IssueErrorIcon)
                    {
                        tooltip = issue.Message.text
                    };
                    if (DrawIconButton(StatusTracker.WarningContextMessages.IssueErrorIcon))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(issue.Message, false, () => issue.Trigger());
                        menu.ShowAsContext();
                    }
                    outVal = returnVal.Invoke();
                }
            }
            return succes.HasValue && !succes.Value;
        }

        //For void methods
        private static bool TryDrawIssue(TrackerIssue issue, Action returnVal)
        {
            bool? succes = issue?.GetLastEvaluation();
            if (succes.HasValue && succes.Value == false)
            {
                using (var hScope = new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                {
                    if (DrawIconButton(StatusTracker.WarningContextMessages.IssueErrorIcon))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(issue.Message, false, () => issue.Trigger());
                        menu.ShowAsContext();
                    }
                    returnVal.Invoke();
                }
            }
            return succes.HasValue && !succes.Value;
        }

        public static void DrawHeader(string text)
        {
            using (var vScope = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(text, Styles.Header);
            }
        }

        public static void DrawSectionHeader(string text, Action UICallback = null)
        {
            GUILayout.Space(SectionHeaderTopSpacing);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(SectionHorizontalSpacing);
                EditorGUILayout.LabelField(text, Styles.SectionHeader);
                GUILayout.FlexibleSpace();
                UICallback?.Invoke();
                GUILayout.Space(SectionHorizontalSpacing);
            }
            GUILayout.Space(SectionHeaderBottomSpacing);
        }

        private static void DrawSectionHeader(GUIContent content, Action UICallback = null)
        {
            GUILayout.Space(SectionHeaderTopSpacing);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(SectionHorizontalSpacing);

                var titleWidth = Styles.SectionHeader.CalcSize(content).x;

                EditorGUILayout.LabelField(content, Styles.SectionHeader, GUILayout.Width(titleWidth));

                GUILayout.FlexibleSpace();
                UICallback?.Invoke();
                GUILayout.Space(SectionHorizontalSpacing);
            }
            GUILayout.Space(SectionHeaderBottomSpacing);
        }

        private static void DrawTopLinksSection()
        {
            GUILayout.BeginVertical(GUILayout.MaxWidth(150));
            GUILayout.Space(5);

            DrawTopLinkbutton(GUIContents.devPortal, GUIContents.devPortalText, () => UsefulLinks.DeveloperPortal());
            DrawTopLinkbutton(GUIContents.documentationIcon, GUIContents.documentationText, () => UsefulLinks.Documentation());
            DrawTopLinkbutton(GUIContents.discordLogo, GUIContents.discordText, () => UsefulLinks.Discord());

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        private static void DrawTopLinkbutton(GUIContent button, GUIContent text, Action OnPress)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(20), GUILayout.MaxWidth(150));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(text, EditorStyles.centeredGreyMiniLabel, GUILayout.Height(20)))
            {
                OnPress?.Invoke();
            }
            if (GUILayout.Button(button, EditorStyles.label, GUILayout.Height(20), GUILayout.Width(20)))
            {
                OnPress?.Invoke();
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

    }
}
