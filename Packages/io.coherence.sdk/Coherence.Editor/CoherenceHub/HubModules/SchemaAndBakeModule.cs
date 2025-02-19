// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    [Serializable, HubModule(Priority = 50)]
    public class SchemaAndBakeModule : HubModule
    {
        public class SchemaAndBakeSettings
        {
            private SerializedObject serializedObject;
            public SerializedProperty compileErrorWatchdog;

            public SchemaAsset[] schemas;
            public GUIContent[] schemaContents;
            public GUIContent[] schemaContext;
            public GUIContent schemaBakeFolderContent;

            public string bakeFolder;

            internal void Init()
            {
                Refresh();
            }

            public void Refresh()
            {
                serializedObject = new SerializedObject(ProjectSettings.instance);
                compileErrorWatchdog = serializedObject.FindProperty("compileErrorWatchdog");

                ProjectSettings.instance.PruneSchemas();

                string[] guids = AssetDatabase.FindAssets("a:assets t:Coherence.SchemaAsset");
                schemas = new SchemaAsset[guids.Length];
                schemaContents = new GUIContent[guids.Length];
                schemaContext = new GUIContent[guids.Length];
                for (int i = 0; i < schemas.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    string fileName = Path.GetFileName(path);
                    schemas[i] = AssetDatabase.LoadAssetAtPath<SchemaAsset>(path);
                    schemaContents[i] = EditorGUIUtility.TrTextContent(fileName, path);
                }

                bakeFolder = ProjectSettings.instance.GetSchemaBakeFolderPath();
                schemaBakeFolderContent = EditorGUIUtility.TrTextContent(bakeFolder, "Path to the schema bake folder. You can move and rename this folder freely.");

                if (!string.IsNullOrEmpty(ProjectSettings.instance.LoginToken))
                {
                    if (!string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.OrganizationID))
                    {
                        Portal.PortalLogin.FetchOrgs();

                        if (!string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.ProjectID))
                        {
                            Portal.Schemas.UpdateSyncState();
                        }
                    }
                }
            }
        }

        public static class ModuleGUIContents
        {
            public static readonly GUIContent bakeFolder = EditorGUIUtility.TrTextContent("Output Folder", "Folder where baked scripts will get placed.");

            public static readonly GUIContent AutoBakingInfo = new GUIContent("Enabling autobaking will keep everything always up-to-date, but may introduce a delay when entering Play Mode or building.");
            public static readonly GUIContent AutoUpload = new GUIContent("Enabling automatic upload of schemas will make sure that they are identical locally and in coherence Cloud. Be aware that this might affect other developers currently working on the same project.");

            public static readonly GUIContent HeaderFolded = new GUIContent("Baking takes the schema and generates optimized network code for your project.");
            public static readonly GUIContent HeaderUnfolded = new GUIContent("Baking is a two-step process. First, it gathers info on your networkable Prefabs and creates a schema based on that. Schemas define what is synchronized over the network between the Client or Simulator and the Replication Server. Then, it generates optimized network code for your project. When baking is completed, you can upload the schema to the coherence Cloud so you can use the online Replication server.");
        }

        public override HelpSection Help => new HelpSection()
        {
            title = ModuleGUIContents.HeaderFolded,
            content = ModuleGUIContents.HeaderUnfolded,
        };

        public override string ModuleName => "Baking";

        private SchemaAndBakeSettings settings = new SchemaAndBakeSettings();

        public override void OnModuleEnable()
        {
            base.OnModuleEnable();
            settings.Init();
        }

        public override void OnModuleDisable()
        {
            base.OnModuleDisable();
        }

        protected override void OnFocusModule()
        {
            settings.Refresh();
        }

        public override void OnBespokeGUI()
        {
            base.OnBespokeGUI();
            CoherenceHubLayout.DrawSection("Baking", DrawBakeSection);
            CoherenceHubLayout.DrawSection("Automate Baking", () =>
            {
                CoherenceHubLayout.DrawInfoLabel(ModuleGUIContents.AutoBakingInfo);
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    SharedModuleSections.DrawAutoBakeSection();
                }
            });

            CoherenceHubLayout.DrawSection("Schemas", DrawSchemaOptions);
            CoherenceHubLayout.DrawSection("Auto Upload Schema", DrawAutoUploadSection);
        }

        private void DrawBakeSection()
        {
            SharedModuleSections.DrawBakeAction();
        }

        private void DrawSchemaOptions()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                SharedModuleSections.DrawSchemasInPortal();
            }
        }

        private void DrawAutoUploadSection()
        {
            CoherenceHubLayout.DrawInfoLabel(ModuleGUIContents.AutoUpload);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                SharedModuleSections.DrawAutoUploadSection();
            }
        }
    }
}
