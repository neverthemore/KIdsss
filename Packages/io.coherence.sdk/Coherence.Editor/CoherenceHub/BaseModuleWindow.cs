// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Toolkit;
    using UnityEditor;

    public class BaseModuleWindow : EditorWindow
    {
        internal HubModule module;
        private CoherenceHeader headerDrawer;

        public HubModule Module => module;

        protected virtual void Awake()
        {
            // see ModuleWindow
        }

        protected virtual void OnEnable()
        {
            InitHeader();
            titleContent = module.TitleContent;
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnDestroy()
        {
            HubModuleManager.instance.ReleaseModule(module);
        }

        protected virtual void OnGUI()
        {
            InitHeader();
            headerDrawer.OnGUI();

            ContentUtils.DrawCloneModeMessage();

            if (module)
            {
                EditorGUI.BeginDisabledGroup(CloneMode.Enabled && !CloneMode.AllowEdits);
                module.DrawModuleGUI();
                EditorGUI.EndDisabledGroup();
            }
        }

        public virtual void OnModuleUpdated()
        {
        }

        private void InitHeader()
        {
            if (headerDrawer == null)
            {
                headerDrawer = new CoherenceHeader(this);
            }
        }
    }
}
