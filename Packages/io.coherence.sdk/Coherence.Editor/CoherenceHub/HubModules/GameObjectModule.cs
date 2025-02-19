// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Editor.Toolkit;
    using Coherence.Toolkit;
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    [Serializable, HubModule(Priority = 80)]
    public class GameObjectModule : HubModule
    {
        protected class ModuleGUIContents
        {
            public static readonly GUIContent SelectPrefab = new GUIContent("Select a Prefab in hierarchy or project to setup coherence");
            public static readonly GUIContent SyncDescription = new GUIContent($"Every GameObject or Prefab that you want to be visible to other players on the network needs a {nameof(CoherenceSync)} script.");
            public static readonly GUIContent ConfigureDescription = new GUIContent($"After adding {nameof(CoherenceSync)}, you can configure which fields and methods to make avaliable over the network and how the object will behave.");
            public static readonly GUIContent OptimizeDescription = new GUIContent("Networking consumes bandwidth, which is always limited.  Here you can configure various optimization techniques for bandwidth and latency optimizations.");
            public static readonly GUIContent ServerSideDescription = new GUIContent("Add client-side input with server-side simulation to your object to make sure it is cheat-proof.");
            public static readonly GUIContent PrefabMapperDescription = new GUIContent("The Prefab Mapper allows you to spawn networked objects that are not in the Resources folder.");

            public static readonly GUIContent SettingUpGameObjects = new GUIContent($"Follow a few simple steps below to setup your Prefabs to be automatically synced by coherence. You just need to add a {nameof(CoherenceSync)} script to a Prefab, select how to load, and coherence will take care of the rest.");
        }

        public override string ModuleName => "GameObject";

        public override HelpSection Help => new HelpSection()
        {
            title = new GUIContent("Setting up GameObjects"),
            content = ModuleGUIContents.SettingUpGameObjects
        };

        public override void OnBespokeGUI()
        {
            base.OnBespokeGUI();
            CoherenceHubLayout.DrawSection("Selected", DrawSelected);
            if (SelectionIsPrefab)
            {
                CoherenceHubLayout.DrawValidatedSection(new GUIContent(nameof(CoherenceSync)), ()=> SelectionHasSyncComponent, DrawCoherenceSync, DrawCoherenceSync);
                CoherenceHubLayout.DrawSection("Configure", DrawConfigure);
                CoherenceHubLayout.DrawSection("Optimize", DrawOptimize);
                CoherenceHubLayout.DrawSection("Server-side Simulation", DrawServerSideSim);
            }

            GUILayout.FlexibleSpace();
        }

        private bool SelectionIsPrefab => Selection.activeGameObject != null && PrefabUtility.GetPrefabAssetType(Selection.activeGameObject) != PrefabAssetType.NotAPrefab;
        private bool SelectionPrefabIsInstance => SelectionIsPrefab && PrefabUtility.IsPartOfPrefabInstance(Selection.activeGameObject);
        private bool SelectionPrefabIsAsset => SelectionIsPrefab && PrefabUtility.IsPartOfPrefabAsset(Selection.activeGameObject);
        private GameObject SelectionPrefabRoot => SelectionPrefabIsInstance ? PrefabUtility.GetNearestPrefabInstanceRoot(Selection.activeGameObject) : SelectionPrefabIsAsset ? Selection.activeGameObject : null;
        private CoherenceSync SelectionSyncComponent => SelectionPrefabRoot?.GetComponent<CoherenceSync>();
        private bool SelectionHasSyncComponent => SelectionSyncComponent != null;

        private void DrawSelected()
        {
            using (var scope = new EditorGUILayout.HorizontalScope())
            {
                if (SelectionIsPrefab)
                {
                    using (new EditorGUILayout.HorizontalScope("box"))
                    {
                        int iconSize = 40;
                        Texture2D image = AssetPreview.GetAssetPreview(SelectionPrefabRoot);
                        GUILayout.Box(image, GUILayout.Height(iconSize), GUILayout.Width(iconSize));
                        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true), GUILayout.Height(iconSize)))
                        {
                            GUILayout.FlexibleSpace();
                            CoherenceHubLayout.DrawLargeLabel(Selection.activeGameObject.name);
                            GUILayout.FlexibleSpace();
                        }
                    }
                }
                else
                {
                    CoherenceHubLayout.DrawMessageArea(ModuleGUIContents.SelectPrefab);
                }
            }
        }

        private void DrawCoherenceSync()
        {
            CoherenceHubLayout.DrawInfoLabel(ModuleGUIContents.SyncDescription);

            if (!SelectionHasSyncComponent)
            {
                CoherenceHubLayout.DrawMessageArea($"If you want this gameobject to be synced, it needs a {nameof(CoherenceSync)} component");
                EditorGUI.BeginDisabledGroup(!SelectionIsPrefab);
                if (CoherenceHubLayout.DrawButton($"Add {nameof(CoherenceSync)}"))
                {
                    ObjectFactory.AddComponent<CoherenceSync>(SelectionPrefabRoot);

                    MessageQueue.AddToQueue(StatusTrackerConstructor.Scopes.ProjectStatus,
                    EditorTasks.StartTask(StatusTrackerConstructor.StatusTrackerIDs.CoherenceSyncAdded),
                    () => CoherenceHubLayout.DrawLabel(new GUIContent($"{nameof(CoherenceSync)} was added to {SelectionPrefabRoot.name}")));
                }
                EditorGUI.EndDisabledGroup();
            }

        }

        private void DrawConfigure()
        {
            CoherenceHubLayout.DrawInfoLabel(ModuleGUIContents.ConfigureDescription);

            EditorGUI.BeginDisabledGroup(!SelectionHasSyncComponent);
            if (CoherenceHubLayout.DrawButton($"Configure"))
            {
                _ = CoherenceSyncBindingsWindow.GetWindow(SelectionSyncComponent);
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawOptimize()
        {
            CoherenceHubLayout.DrawInfoLabel(ModuleGUIContents.OptimizeDescription);

            EditorGUI.BeginDisabledGroup(!SelectionHasSyncComponent);
            if (CoherenceHubLayout.DrawButton("Optimize"))
            {
                CoherenceMainMenu.OpenBindingsWindow();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawServerSideSim()
        {
            CoherenceHubLayout.DrawInfoLabel(ModuleGUIContents.ServerSideDescription);

            EditorGUI.BeginDisabledGroup(!SelectionHasSyncComponent);

            if (!SelectionPrefabRoot?.GetComponent<CoherenceInput>())
            {
                if (CoherenceHubLayout.DrawButton("Add input"))
                {
                    ObjectFactory.AddComponent<CoherenceInput>(SelectionPrefabRoot);

                    MessageQueue.AddToQueue(StatusTrackerConstructor.Scopes.ProjectStatus,
                    EditorTasks.StartTask(StatusTrackerConstructor.StatusTrackerIDs.CoherenceInputAdded),
                    () => CoherenceHubLayout.DrawLabel(new GUIContent($"{nameof(CoherenceInput)} was added to {SelectionPrefabRoot.name}")));
                }
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
