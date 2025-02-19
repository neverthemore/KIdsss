// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using Coherence.Toolkit;
    using UnityEditor;

    [Serializable, HubModule(Priority = 60)]
    public class CoherenceSyncObjectsModule : HubModule
    {
        protected override bool UseSearchField => false;
        protected override bool UseScroll => false;
        public override string ModuleName => "CoherenceSync Objects";

        private BaseEditor registryEditor;

        public override void OnModuleEnable()
        {
            base.OnModuleEnable();
            registryEditor = Editor.CreateEditor(CoherenceSyncConfigRegistry.Instance) as BaseEditor;
            if (registryEditor != null)
            {
                registryEditor.DrawCustomHeader = false;
            }
        }

        public override void OnModuleDisable()
        {
            DestroyImmediate(registryEditor);
        }

        public override void OnGUI()
        {
            base.OnBespokeGUI();
            registryEditor.OnInspectorGUI();
        }
    }
}
