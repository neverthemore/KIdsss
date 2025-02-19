// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using UnityEditor.IMGUI.Controls;

    [Serializable]
    public class HubModule : ScriptableObject
    {
        public class HelpSection
        {
            public GUIContent title;
            public GUIContent content;
        }

        internal class GUIContents
        {
            public static readonly GUIContent UnDock = Icons.GetContent("d_Coherence.HubModule.Undock", "Undock window, allowing you to dock it where you see fit");
            public static readonly GUIContent HelpLight = EditorGUIUtility.TrIconContent("d__Help", "Open relevant documentation to learn more");
            public static readonly GUIContent Help = EditorGUIUtility.TrIconContent("_Help", "Open relevant documentation to learn more");
            public static readonly GUIContent Settings = new GUIContent("Settings");
            public static readonly GUIContent SettingsIcon = EditorGUIUtility.TrIconContent("d_Settings", "Open project settings for additional configuration options");

            public static readonly Dictionary<Type, DocumentationKeys> ModuleDocumentationLinks = new Dictionary<Type, DocumentationKeys>()
            {
                { typeof(OverviewModule), DocumentationKeys.None },
                { typeof(OnlineModule), DocumentationKeys.DeveloperPortalOverview },
                { typeof(GameObjectModule), DocumentationKeys.PrefabSetup },
                { typeof(SceneModule), DocumentationKeys.SceneSetup },
                { typeof(SchemaAndBakeModule), DocumentationKeys.Baking },
                { typeof(SimulatorsModule), DocumentationKeys.Simulators },
                { typeof(LocalServerModule), DocumentationKeys.LocalServers },
                { typeof(MultiRoomSimulatorsWizardModule), DocumentationKeys.MRS }
            };
        }

        [SerializeField] private EditorWindow host;

        public EditorWindow Host
        {
            get => host;
            internal set => host = value;
        }

        private readonly bool allowsUnDocking = true;
        public virtual GUIContent TitleContent => new(ModuleName);
        public virtual string ModuleName => GetType().Name;

        public virtual HelpSection Help => null;

        [field: SerializeField]
        public bool IsDocked { get; set; } = true;
        private Type wrapperType;

        private Vector2 scrollPosition;
        internal bool ShowHelpSections;

        private SearchField searchField;
        protected string searchText = string.Empty;

        protected virtual bool UseScroll => true;
        protected virtual bool UseSearchField => false;

        public virtual void OnBespokeGUI() { }

        public virtual void OnGUI() { }

        private void OnEnable()
        {
            CoherenceHub.OnHubGainedFocus -= CheckModuleFocus;
            CoherenceHub.OnHubGainedFocus += CheckModuleFocus;
            OnModuleEnable();
            searchField = new SearchField();
        }

        public virtual void OnModuleEnable() => wrapperType = GetWrapperType();
        internal virtual void OnRegisteredByHub() { }
        internal virtual void OnRegisteredByModuleWindow() { }

        private void OnDisable()
        {
            CoherenceHub.OnHubGainedFocus -= CheckModuleFocus;
            OnModuleDisable();
        }

        private void CheckModuleFocus()
        {
            //Todo need a check to see if specific module is open
            OnFocusModule();
        }

        protected virtual void OnFocusModule() { }
        public virtual void OnModuleDisable() { }

        internal void DrawModuleGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var search = UseSearchField ? searchField : null;
                searchText = CoherenceHubLayout.DrawModuleFrame(this, searchText, search);
            }

            OnGUI();

            if (UseScroll)
            {
                using (var scrollArea = new EditorGUILayout.ScrollViewScope(scrollPosition))
                {
                    scrollPosition = scrollArea.scrollPosition;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(CoherenceHub.OuterSpacerH);
                        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
                        {
                            if (Help != null)
                            {
                                ShowHelpSections = CoherenceHubLayout.DrawHelpFoldout(ShowHelpSections, Help);
                            }

                            OnBespokeGUI();
                        }
                        GUILayout.Space(CoherenceHub.OuterSpacerH);
                    }
                }


                //Draws the messageQueue from a given scope
                if (MessageQueue.HasMessages(StatusTrackerConstructor.Scopes.ProjectStatus))
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.VerticalScope(CoherenceHubLayout.Styles.SectionBox))
                    {
                        MessageQueue.ProcessActiveMessages(StatusTrackerConstructor.Scopes.ProjectStatus);
                    }
                }
            }
        }

        private Type GetWrapperType()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<IModuleWindow>().Where(x => !x.IsGenericType))
            {
                if (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(ModuleWindow<,>))
                {
                    //We have a corresponding window
                    if (type.BaseType.GetGenericArguments().Contains(GetType()))
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="shouldOpen"></param>
        /// <returns>Did we succesfully open a window</returns>
        public bool OpenWindowWrapper(bool shouldOpen)
        {
            if (wrapperType != null)
            {
                var method = wrapperType.GetMethod("OpenWindow", BindingFlags.Public |
                    BindingFlags.Static |
                    BindingFlags.FlattenHierarchy);

                var methodParams = new object[1] { shouldOpen };
                method.Invoke(null, methodParams);

                return true;
            }

            Debug.Log($"No custom window for {GetType().Name} exists");
            return false;
        }

        public bool AllowsUndocking => allowsUnDocking && wrapperType != null;

        public void Dock(bool shouldDock)
        {
            if (!shouldDock)
            {
                CoherenceHub.ResetTabSelection();
            }

            if (OpenWindowWrapper(!shouldDock))
            {
                IsDocked = shouldDock;
            }
        }

        protected void Repaint()
        {
            if(host)
            {
                host.Repaint();
            }
        }
    }
}
