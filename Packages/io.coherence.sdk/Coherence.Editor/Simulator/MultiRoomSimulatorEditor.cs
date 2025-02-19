// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEditor;

    [CustomEditor(typeof(Simulator.MultiRoomSimulator)), CanEditMultipleObjects]
    internal class MultiRoomSimulatorEditor : BaseEditor
    {
        protected override string Description => $"Enables Multi-Room Simulator support by listening to request @ PUT localhost:{ProjectSettings.instance.RuntimeSettings.LocalHttpServerPort}/rooms. Requests trigger scene loading via {nameof(Coherence.Toolkit.CoherenceSceneLoader)}. For local development, use {nameof(Simulator.MultiRoomSimulatorLocalForwarder)} to send requests.";

        protected override void OnGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
