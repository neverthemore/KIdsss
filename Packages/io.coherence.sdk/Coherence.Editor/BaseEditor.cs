// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#define COHERENCE_SLIM_HEADER

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    internal class BaseEditor : Editor
    {
        protected virtual string Description { get; }
        protected virtual MenuItemData[] MenuItems { get; }

        public bool DrawCustomHeader { get; set; } = true;

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        protected virtual bool UseMargins()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            var useMargins = UseMargins();

            if (DrawCustomHeader)
            {
                CoherenceHeader.DrawSlimHeader(Description, target, MenuItems);
            }

            OnAfterHeader();

            if (useMargins)
            {
                _ = EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            }

            var cloneMode = GUI.enabled && CloneMode.Enabled;
            if (cloneMode)
            {
                var allowEditsInPlayMode = false;
                foreach (var target in targets)
                {
                    if (!EditorUtility.IsPersistent(target))
                    {
                        allowEditsInPlayMode = true;
                        break;
                    }
                }

                ContentUtils.DrawCloneModeMessage(allowEditsInPlayMode);
                EditorGUILayout.Space();
                var enabled = Application.isPlaying && (allowEditsInPlayMode || CloneMode.AllowEdits) ||
                              !Application.isPlaying && CloneMode.AllowEdits;
                EditorGUI.BeginDisabledGroup(!enabled);
            }

            OnGUI();

            if (cloneMode)
            {
                EditorGUI.EndDisabledGroup();
            }

            if (useMargins)
            {
                EditorGUILayout.EndVertical();
            }
        }

        protected virtual void OnAfterHeader()
        {
        }

        protected virtual void OnGUI()
        {
        }
    }
}
