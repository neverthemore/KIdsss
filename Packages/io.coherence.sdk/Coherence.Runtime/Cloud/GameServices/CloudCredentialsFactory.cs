// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Common;
#if UNITY
    using UnityEngine;
#endif

    internal static class CloudCredentialsFactory
    {
#if UNITY
        private static CloudCredentialsPair sharedSimulatorCredentials;
#endif

#if UNITY_EDITOR
        // Support Enter Play Mode Options: Disable Reload Domain in the Unity Editor
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => sharedSimulatorCredentials = null;
#endif

        internal static CloudCredentialsPair ForClient(IRuntimeSettings runtimeSettings) => ForClient(runtimeSettings, "", false);

        [Deprecated("15/10/2024", 1, 4, 0, Reason="coherence/unity#6843")]
        internal static CloudCredentialsPair ForClient(IRuntimeSettings runtimeSettings, string uniqueId = "", bool autoLoginAsGuest = false)
        {
            var useWebsocket = true;
#if UNITY
            useWebsocket = Application.platform != RuntimePlatform.Switch;
#endif

            var newRequestFactory = new RequestFactory(runtimeSettings, useWebsocket);
            var newAuthClient = AuthClient.ForPlayer(newRequestFactory, runtimeSettings.ProjectID, uniqueId:uniqueId, autoLoginAsGuest:autoLoginAsGuest);
            return new(newAuthClient, newRequestFactory);
        }

#if UNITY
        internal static CloudCredentialsPair ForSimulator(IRuntimeSettings runtimeSettings)
        {
            if (sharedSimulatorCredentials != null)
            {
                return sharedSimulatorCredentials;
            }

            var newRequestFactory = new RequestFactory(runtimeSettings);
            var newAuthClient = AuthClient.ForSimulator(newRequestFactory, runtimeSettings.ProjectID);

            var credentialsPair = new CloudCredentialsPair(newAuthClient, newRequestFactory);

            // If we're fed an authentication token for a simulator, we force a single AuthClient/WebSocket instance for every CloudService
            if (SimulatorUtility.UseSharedCloudCredentials)
            {
                sharedSimulatorCredentials = credentialsPair;
            }

            return credentialsPair;
        }
#endif
    }
}
