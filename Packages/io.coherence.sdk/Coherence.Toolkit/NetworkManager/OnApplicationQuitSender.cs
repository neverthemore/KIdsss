namespace Coherence.Toolkit
{
    using UnityEngine;

    /// <summary>
    /// Triggers a <see cref="CoherenceSyncConfigRegistry.CleanUp()"/> on OnApplicationQuit.
    /// </summary>
    /// <remarks>
    /// An instance is created at runtime via <see cref="RuntimeInitializeOnLoadMethodAttribute"/>,
    /// and set to <see cref="Object.DontDestroyOnLoad"/>.
    /// </remarks>
    /// <seealso cref="MonoBehaviour"/>
    [DefaultExecutionOrder(ScriptExecutionOrder.OnApplicationQuitSender)]
    public class OnApplicationQuitSender : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        internal static void InstantiateSender()
        {
            var go = new GameObject(nameof(OnApplicationQuitSender));
            go.AddComponent<OnApplicationQuitSender>();
            DontDestroyOnLoad(go);
        }

        private void OnApplicationQuit()
        {
            CoherenceSyncConfigRegistry.Instance.CleanUp();
        }
    }
}
