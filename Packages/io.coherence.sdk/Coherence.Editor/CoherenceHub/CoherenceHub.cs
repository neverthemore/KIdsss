// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System;
    using System.Linq;
    using Toolkit;
    using UnityEngine.Networking;

    public class CoherenceHub : EditorWindow, IAnyHubModule
    {
        private class HubSectionClickedProperties : Analytics.BaseProperties
        {
            public string section;
        }

        [Serializable]
        public class VersionInfo
        {
            public string SDKVersionString => $"v{RuntimeSettings.Instance?.VersionInfo?.Sdk}";

            public Version SDKVersion => Version.TryParse(RuntimeSettings.Instance?.VersionInfo?.Sdk, out var curVersion) ? curVersion : null;

            public Version AvailableVersion = null;

            public bool IsLatestVersion => AvailableVersion == null || SDKVersion == null || SDKVersion >= AvailableVersion;

            public string EngineVersion => RuntimeSettings.Instance.VersionInfo?.Engine;
            public string ProjectID => RuntimeSettings.Instance.ProjectID;
            public string SchemaID => RuntimeSettings.Instance.SchemaID;

            public UnityWebRequest SDKVersionRequest { get; private set; }

            public override string ToString()
            {
                return SDKVersionString;
            }

            public void CopyToClipBoard()
            {
                var versionData =
                    $"SDK: {SDKVersionString}{Environment.NewLine}" +
                    $"Engine: {EngineVersion}{Environment.NewLine}" +
                    $"Unity: {Application.unityVersion}{Environment.NewLine}" +
                    $"OS: {SystemInfo.operatingSystem}{Environment.NewLine}" +
                    $"ProjectID: {ProjectID}{Environment.NewLine}" +
                    $"SchemaID: {SchemaID}{Environment.NewLine}";

                GUIUtility.systemCopyBuffer = versionData;

                var window = EditorWindow.focusedWindow;
                if (window)
                {
                    window.ShowNotification(new GUIContent("Copied to clipboard"));
                }
            }

            internal void GetLatestVersion()
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CoherenceHub).Assembly);
                Version.TryParse(package.versions.latestCompatible, out AvailableVersion);
            }
        }

        public static Action OnHubGainedFocus;
        public static readonly VersionInfo info = new VersionInfo();

        public Log.Logger Logger { get; set; }
        public const float OuterSpacerH = 8;
        public const float OuterSpacerV = 6;

        internal const string iconPathEditorWindow = "EditorWindow";
        private const string windowTitle = "coherence Hub";
        private const string TabIndexKey = "Coherence.Hub.TabIndex";

        private Vector2 scrollPos;
        private int tabIndex;
        private GUIContent[] ToolbarContent => GetDockedModules().Select(m => m.TitleContent).ToArray();
        private CoherenceHeader headerDrawer;

        private void Awake()
        {
            _ = HubModuleManager.instance.Purge();
        }

        internal static void Open()
        {
            var window = GetWindow<CoherenceHub>(windowTitle);
            window.titleContent = GetTextContentWithIcon(windowTitle, iconPathEditorWindow);
        }

        private void OnEnable()
        {
            InitTrackers();

            InitHeader();
            // We want to receive MouseMove events in order to show on-hover popups without window flickering, since MouseMove events do not trigger repaints
            wantsMouseMove = true;
            HubModuleManager.instance.AssignAllModules(this, HubModuleManager.AssignStrategy.IgnoreAssiged);

            tabIndex = EditorPrefs.GetInt(TabIndexKey, 0);
            CheckTabIndexBoundary();

            info.GetLatestVersion();
            EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
        }

        private static void InitTrackers()
        {
            if (StatusTrackerController.instance.Trackers.Count != 0)
            {
                return;
            }

            StatusTrackerConstructor.InitTrackers();
        }

        private void CheckTabIndexBoundary()
        {
            if (tabIndex >= HubModuleManager.instance.GetActiveModules(this).Count())
            {
                tabIndex = 0;
            }
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandleOnPlayModeChanged;
            EditorPrefs.SetInt(TabIndexKey, tabIndex);
        }

        private void OnDestroy()
        {
            HubModuleManager.instance.ReleaseModules(this);
        }

        private void OnFocus()
        {
            OnHubGainedFocus?.Invoke();
        }

        private static void HandleOnPlayModeChanged(PlayModeStateChange obj)
        {
            // TODO RegisterModules();
        }

        internal static void FocusModule<T>() where T : HubModule
        {
            // TODO
            var module = GetModule<T>();
            if (module.IsDocked)
            {
                GetWindow<CoherenceHub>().Focus(module);
            }
            else
            {
                _ = module.OpenWindowWrapper(true);
            }
        }

        private void Focus<T>(T module) where T : HubModule
        {
            tabIndex = HubModuleManager.instance.GetActiveModules(this).ToList().IndexOf(module);
        }

        internal static void ResetTabSelection()
        {
            GetWindow<CoherenceHub>().tabIndex = 0;
        }

        public static T GetModule<T>() where T : HubModule
        {
            return HubModuleManager.instance.GetActiveModule<T>();
        }

        private void InitHeader()
        {
            if (headerDrawer == null)
            {
                headerDrawer = new CoherenceHeader(this);
            }
        }

        private void OnGUI()
        {
            InitTrackers();
            InitHeader();
            headerDrawer.OnGUIWithLogin();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(OuterSpacerH);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(OuterSpacerV);
                    DoToolbar();
                    GUILayout.Space(OuterSpacerV);
                }
                GUILayout.Space(OuterSpacerH);
            }

            DoWindow();

            //Added this because custom guiskins update slower (Hover not being immediately called
            Repaint();
        }

        internal static GUIContent GetTextContentWithIcon(string title, string iconPath, string tooltip = null)
        {
            return EditorGUIUtility.TrTextContentWithIcon(title, tooltip, Icons.GetPath(iconPath));
        }

        private void DoToolbar()
        {
            EditorGUI.BeginChangeCheck();
            tabIndex = CoherenceHubLayout.DrawGrid(tabIndex, ToolbarContent);
            if (EditorGUI.EndChangeCheck())
            {
                var module = GetDockedModules().ElementAtOrDefault(tabIndex);
                if (module != null)
                {
                    Analytics.Capture(new Analytics.Event<HubSectionClickedProperties>(
                        Analytics.Events.HubSectionClicked,
                        new HubSectionClickedProperties
                        {
                            section = module.ModuleName,
                        }
                    ));
                }
            }
        }

        private void DoWindow()
        {
            using var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPos);
            scrollPos = scrollScope.scrollPosition;
            var dockedModules = GetDockedModules().ToArray();
            if (dockedModules.Length < (tabIndex + 1))
            {
                tabIndex = 0;
                return;
            }

            var module = dockedModules[tabIndex];
            if (module)
            {
                ContentUtils.DrawCloneModeMessage();
                EditorGUI.BeginDisabledGroup(CloneMode.Enabled && !CloneMode.AllowEdits);
                module.DrawModuleGUI();
                EditorGUI.EndDisabledGroup();
            }
        }

        private IEnumerable<HubModule> GetDockedModules()
        {
            return HubModuleManager.instance.GetActiveModules(this);
        }
    }
}
