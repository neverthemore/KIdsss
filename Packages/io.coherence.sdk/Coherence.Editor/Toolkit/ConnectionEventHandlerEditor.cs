// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEditor;

    [CustomEditor(typeof(Coherence.Toolkit.ConnectionEventHandler)), CanEditMultipleObjects]
    internal class ConnectionEventHandlerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            CoherenceHeader.OnSlimHeader(string.Empty);

            EditorGUILayout.LabelField("Fires events on connections and disconnections.", ContentUtils.GUIStyles.miniLabelGreyWrap);

            EditorGUILayout.Space();

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
