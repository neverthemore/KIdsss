// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Editor.Toolkit;
    using Coherence.Toolkit;
    using Coherence.UI;
    using System;
    using UnityEditor;
    using UnityEngine;

    [Serializable, HubModule(Priority = 70)]
    public class SceneModule : HubModule
    {
        private class ModuleGUIContents
        {
            public static readonly SectionData BridgeSection = new SectionData("CoherenceBridge (required)", "Create CoherenceBridge", "CoherenceBridge in Scene", "Keeps track of Entities and Game Objects. Every multiplayer scene needs one.");
            public static readonly SectionData LiveQuerySection = new SectionData("LiveQuery (required)", "Add LiveQuery to Scene", "LiveQuery in Scene", "Defines an area of interest within the scene. Every multiplayer scene needs one. You can configure the LiveQuery after adding it to the scene.");
            public static readonly SectionData SampleUISection = new SectionData(
                "Connect UI Dialogs",
                "Add a Connect Dialog Sample",
                "",
                "In order to connect to a Replication Server hosted in the coherence Cloud or locally, coherence offers Sample implementations of Connect Dialog UIs for Rooms, Worlds or Lobbies." +
                "\n\nThese Samples can be imported into your Project and instanced in your networked Scene." +
                "\n\nYou will be able to learn at your own pace, whilst having a UI readily available to be able to test your game fast. ",
                linkText: "Learn More",
                linkUrl: DocumentationKeys.RoomsAndWorlds);

            public static readonly GUIContent SettingUpScene = new GUIContent("In order for coherence to sync your GameObjects correctly, you need to add a few objects to the scene. Simply follow the steps below, and you are ready to go." +
                "\nWe even provide a UI Prefab to manage connections to Servers. This, however is meant for production purposes - for release builds you should create your own UI.");
        }

        private struct SectionData
        {
            public string Header { get; }
            public string ButtonText { get; }
            public string ButtonDisabledText { get; }
            public GUIContent SectionText { get; }
            public string LinkText { get; }
            public DocumentationKeys LinkUrl { get; }

            public SectionData(string header, string buttonText, string buttonDisabledText, string sectionText, string linkText = null, DocumentationKeys linkUrl = DocumentationKeys.None)
            {
                Header = header;
                ButtonText = buttonText;
                ButtonDisabledText = buttonDisabledText;
                SectionText = new GUIContent(sectionText);
                LinkText = linkText;
                LinkUrl = linkUrl;
            }
        }

        public override string ModuleName => "Scene";

        public override HelpSection Help => new HelpSection()
        {
            title = new GUIContent("How to Use a Scene with coherence"),
            content = ModuleGUIContents.SettingUpScene
        };

#if UNITY_2023_1_OR_NEWER
        public bool MissingBridge => FindAnyObjectByType<CoherenceBridge>() == null;
        public bool MissingLiveQuery => FindAnyObjectByType<CoherenceLiveQuery>() == null;
#else
        public bool MissingBridge => FindObjectOfType<CoherenceBridge>() == null;
        public bool MissingLiveQuery => FindObjectOfType<CoherenceLiveQuery>() == null;
#endif

        public override void OnBespokeGUI()
        {
            base.OnBespokeGUI();

            Draw(ModuleGUIContents.BridgeSection, MissingBridge, () => Utils.AddBridgeInstanceInScene(null));
            Draw(ModuleGUIContents.LiveQuerySection, MissingLiveQuery, () => Utils.AddLiveQueryInstanceInScene(null));
            Draw(ModuleGUIContents.SampleUISection, true, () => SampleDialogPickerWindow.ShowWindow("Connect Dialog"), true);

            // TODO VladN 2023-03-09: removed MRS simulator link from the scene setup
            // Temporary removed LocalServerModule, so have to draw manually
            // CoherenceHubLayout.DrawSection("Multi-room Simulators", () => CoherenceHub.GetModule<LocalServerModule>().DrawMRS());
            // CoherenceHubLayout.DrawSection("Multi-Room Simulators (advanced)", () =>
            // {
            //     CoherenceHubLayout.DrawInfoLabel(LocalServerModule.ModuleGUIContents.MRSDescription);
            //     if (CoherenceHubLayout.DrawButton(LocalServerModule.ModuleGUIContents.MRSButton))
            //     {
            //         CoherenceMainMenu.OpenMRSWizard();
            //     }
            // });
        }

        private void Draw(SectionData sectiondata, bool enabled, Action onPressed, bool optional = false, params (string text, Action onClick)[] buttons)
        {
            CoherenceHubLayout.DrawSection(sectiondata.Header, () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    CoherenceHubLayout.DrawInfoLabel(sectiondata.SectionText);
                    if (sectiondata.LinkUrl != DocumentationKeys.None)
                    {
                        GUILayout.FlexibleSpace();
                        var url = DocumentationLinks.GetDocsUrl(sectiondata.LinkUrl);

                        CoherenceHubLayout.DrawLink(new GUIContent(sectiondata.LinkText ?? sectiondata.LinkUrl.ToString(), url),
                            url);
                    }
                }
                if (buttons.Length > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        foreach ((string text, Action onClick) button in buttons)
                        {
                            if (CoherenceHubLayout.DrawButton(button.text))
                            {
                                button.onClick?.Invoke();
                            }
                        }
                    }
                }

                EditorGUI.BeginDisabledGroup(!enabled);
                if (CoherenceHubLayout.DrawButton(enabled ? sectiondata.ButtonText : sectiondata.ButtonDisabledText))
                {
                    onPressed?.Invoke();
                }
                EditorGUI.EndDisabledGroup();
            });
        }
    }
}
