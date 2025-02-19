// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Coherence.Toolkit;
    using Simulator;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [CustomEditor(typeof(VersionInfo))]
    internal class VersionInfoEditor : Editor
    {
        private class GUIContents
        {
            public static readonly GUIContent fetchVersions = EditorGUIUtility.TrTextContent("Fetch tool versions",
                "Access the Portal to fetch latest available versions of each tool.");

            public static readonly GUIContent editVersionInfo = EditorGUIUtility.TrTextContent("Edit");

            public static readonly GUIContent refreshRevisionHash = EditorGUIUtility.TrTextContent("Refresh");

            public static readonly GUIContent engineLabel =
                EditorGUIUtility.TrTextContent("Tools", "Version of the command line tools to use.");

            public static readonly GUIContent bakeBuiltinSchemas =
                EditorGUIUtility.TrTextContent("Bake Builtin Schemas", "Bake Toolkit and Engine Schemas.");
        }

        private VersionData[] versionDatas;

        private SerializedProperty sdkProperty;
        private SerializedProperty sdkRevisionHashProperty;
        private SerializedProperty engineProperty;
        private SerializedProperty docsSlugProperty;
        private SerializedProperty unpublishedProperty;

        private static Dictionary<Type, DocumentationKeys> helpUrlKeys = new Dictionary<Type, DocumentationKeys>
        {
            { typeof(CoherenceBridge), DocumentationKeys.CoherenceBridge },
            { typeof(CoherenceLiveQuery), DocumentationKeys.LiveQuery },
            { typeof(CoherenceInput), DocumentationKeys.InputQueues },
            { typeof(CoherenceSync), DocumentationKeys.CoherenceSync },
            { typeof(CoherenceTagQuery), DocumentationKeys.TagQuery },
            { typeof(AutoSimulatorConnection), DocumentationKeys.AutoSimulatorConnection }
        };

        private void OnEnable()
        {
            sdkProperty = serializedObject.FindProperty("sdk");
            sdkRevisionHashProperty = serializedObject.FindProperty("sdkRevisionHash");
            engineProperty = serializedObject.FindProperty("engine");
            docsSlugProperty = serializedObject.FindProperty("docsSlug");
            unpublishedProperty = serializedObject.FindProperty("useUnpublishedDocsUrl");

            if (versionDatas == null)
            {
                versionDatas = new[]
                {
                    new VersionData(GUIContents.engineLabel, "engine", engineProperty)
                };
            }
            else
            {
                versionDatas[0].prop = engineProperty;
            }
        }

        private void OnDisable()
        {
            sdkProperty.Dispose();
            sdkRevisionHashProperty.Dispose();
            engineProperty.Dispose();
            docsSlugProperty.Dispose();
            unpublishedProperty.Dispose();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            _ = EditorGUILayout.BeginHorizontal();
            _ = EditorGUILayout.PropertyField(sdkProperty);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(GUIContents.editVersionInfo, EditorStyles.miniButton, GUILayout.Width(36f)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(Paths.packageManifestPath);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            _ = EditorGUILayout.BeginHorizontal();
            _ = EditorGUILayout.PropertyField(sdkRevisionHashProperty);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(GUIContents.refreshRevisionHash, EditorStyles.miniButton, GUILayout.Width(60f)))
            {
                RevisionInfo.GetAndSaveHash();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button(GUIContents.fetchVersions))
            {
                foreach (VersionData vd in versionDatas)
                {
                    _ = vd.FetchReleases();
                }
            }


            EditorGUILayout.Space();

            foreach (VersionData vd in versionDatas)
            {
                vd.OnGUI();
            }

            _ = EditorGUILayout.PropertyField(docsSlugProperty);

            using (new EditorGUILayout.HorizontalScope())
            {
                _ = EditorGUILayout.PropertyField(unpublishedProperty);

                if (GUILayout.Button("Update HelpURL links"))
                {
                    RefreshHelpUrls();
                }
            }

            _ = serializedObject.ApplyModifiedProperties();
        }

        public static void RefreshHelpUrls()
        {
            foreach (var assemblyName in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
            {
                var assembly = Assembly.Load(assemblyName);
                RefreshHelpUrlsForAssembly(assembly);
            }
        }

        private static void RefreshHelpUrlsForAssembly(Assembly assembly)
        {
            var helpUrlsTypes = assembly.GetTypes().Where(t => t.GetCustomAttribute<HelpURLAttribute>() != null);

            bool updatedAny = false;
            GameObject go = new GameObject();

            foreach (var type in helpUrlsTypes)
            {
                if (!helpUrlKeys.TryGetValue(type, out DocumentationKeys docKey))
                {
                    Debug.LogError($"Type {type} not found in helpUrlKeys Dictionary, please add it.");
                    return;
                }

                var attribute = type.GetCustomAttribute<HelpURLAttribute>();
                var comp = go.GetComponent(type) as MonoBehaviour ?? go.AddComponent(type) as MonoBehaviour;
                var monoScript = MonoScript.FromMonoBehaviour(comp);

                if (monoScript == null)
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(monoScript);
                var text = monoScript.text;

                var docsUrl = DocumentationLinks.GetDocsUrl(docKey);

                if (docsUrl.Equals(attribute.URL))
                {
                    continue;
                }

                text = text.Replace($"[HelpURL(\"{attribute.URL}\")]", $"[HelpURL(\"{docsUrl}\")]");
                File.WriteAllText(path, text);
                updatedAny = true;
                Debug.Log($"Updated {type.Name} from {attribute.URL} to {docsUrl}");
            }

            DestroyImmediate(go);

            if (updatedAny)
            {
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log("All HelpURL attributes are up to date!");
            }
        }
    }
}
