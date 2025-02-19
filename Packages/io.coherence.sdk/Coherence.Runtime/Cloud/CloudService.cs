// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
// Any changes to the Unity version of the request should be reflected
// in the HttpClient version.
// TODO: Separate Http client impl. with common options/policy layer (coherence/unity#1764)
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Common;
    using Runtime;
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using static Coherence.Log.Log;

    /// <summary>
    ///     Runtime API to be able to interface with an Organization and Project from the coherence Cloud.
    ///     Check the coherence Cloud tab in the coherence Hub window for more details.
    /// </summary>
    public class CloudService : IAsyncDisposable, IDisposable
    {
        internal const string RequestIDHeader = "X-Coherence-Request-ID";
        internal const string ClientVersionHeader = "X-Coherence-Client";
        internal const string SchemaIdHeader = "X-Coherence-Schema-ID";
        internal const string RSVersionHeader = "X-Coherence-Engine";

        private bool shouldDisposeRequestFactoryAndAuthClient;

        /// <summary>
        ///     Returns true when the Web Socket is connected.
        /// </summary>
        public bool IsConnectedToCloud => requestFactory.IsReady;

        /// <summary>
        ///     Returns true when the Web Socket is connected and when we are logged in to coherence Cloud.
        /// </summary>
        public bool IsLoggedIn
        {
            get
            {
                var requestFactoryReady = requestFactory.IsReady;

                var authClientReady = GameServices.AuthService.LoggedIn;

                return requestFactoryReady && authClientReady;
            }
        }

        /// <summary>
        ///     RuntimeSettings that you pass through the constructor, if none is specified, RuntimeSettings.Instance will be used.
        /// </summary>
        public IRuntimeSettings RuntimeSettings => runtimeSettings;
        /// <summary>
        ///     Worlds REST service to fetch the available worlds that are online in the specified coherence Project from the RuntimeSettings.
        /// </summary>
        public WorldsService Worlds { get; }
        /// <summary>
        ///     Rooms REST service to fetch, create and delete rooms in the specified coherence Project from the RuntimeSettings.
        /// </summary>
        public CloudRooms Rooms { get; }
        /// <summary>
        ///     Collection of Game Services that you can interact with in the coherence Dashboard under the Game Services section of
        ///     your project.
        /// </summary>
        public GameServices GameServices { get; }
        /// <summary>
        ///     GameServers REST service that controls game servers in the specified coherence Project from the RuntimeSettings.
        /// </summary>
        public IGameServersService GameServers { get; }

        internal AnalyticsClient AnalyticsClient { get; private set; }

        private readonly IRequestFactory requestFactory;
        private readonly IAuthClientInternal authClient;
        private readonly IRuntimeSettings runtimeSettings;

        public static CloudService ForClient(IRuntimeSettings runtimeSettings = null)
            => ForClient(runtimeSettings, null, false);

        [Deprecated("15/10/2024", 1, 4, 0, Reason="coherence/unity#6843")]
        internal static CloudService ForClient(IRuntimeSettings runtimeSettings = null, string uniqueId = null, bool autoLoginAsGuest = true)
        {
#if UNITY
            runtimeSettings ??= Coherence.RuntimeSettings.Instance;
#endif
            var credentialsPair = CloudCredentialsFactory.ForClient(runtimeSettings, uniqueId, autoLoginAsGuest);
            return new(credentialsPair, runtimeSettings) { shouldDisposeRequestFactoryAndAuthClient = true };
        }

#if UNITY
        internal static CloudService ForSimulator(IRuntimeSettings runtimeSettings = null)
        {
            runtimeSettings ??= Coherence.RuntimeSettings.Instance;
            var credentialsPair = CloudCredentialsFactory.ForSimulator(runtimeSettings);
            return new(credentialsPair, runtimeSettings) { shouldDisposeRequestFactoryAndAuthClient = !SimulatorUtility.UseSharedCloudCredentials };
        }
#endif

        [Obsolete("This constructor will be removed in a future version. " + nameof(ForClient) + " should be used instead.")]
        [Deprecated("08/2024", 1, 3, 1)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CloudService(string uniqueId = null, bool autoLoginAsGuest = true, IRuntimeSettings runtimeSettings = null)
            : this(CloudCredentialsFactory.ForClient(runtimeSettings, uniqueId, autoLoginAsGuest), runtimeSettings)
                => shouldDisposeRequestFactoryAndAuthClient = true;

        private CloudService(CloudCredentialsPair credentials, IRuntimeSettings runtimeSettings)
        {
            this.runtimeSettings = runtimeSettings;
            authClient = credentials.authClient;
            requestFactory = credentials.RequestFactory;
            GameServices = new GameServices(credentials);
            Rooms = new CloudRooms(credentials, RuntimeSettings);
            Worlds = new WorldsService(credentials, RuntimeSettings);
            AnalyticsClient = new AnalyticsClient(credentials, RuntimeSettings);
            GameServers = new GameServersService(credentials, RuntimeSettings);
        }

        /// <summary>
        ///     IEnumerator you can use to wait for the CloudService to be ready within a Coroutine.
        /// </summary>
        public IEnumerator WaitForCloudServiceLoginRoutine()
        {
            while (!IsLoggedIn)
            {
                yield return null;
            }
        }

        /// <summary>
        ///     Async method you can use to wait for the CloudService to be ready.
        /// </summary>
        /// <returns>Returns true when the CloudService is ready.</returns>
        public async Task<bool> WaitForCloudServiceLoginAsync(int millisecondsPollDelay)
        {
            while (!IsLoggedIn)
            {
                await Task.Delay(millisecondsPollDelay);
            }

            return true;
        }

        public void Dispose()
        {
            GameServices.Dispose();
            Rooms.Dispose();
            Worlds.Dispose();

            if (shouldDisposeRequestFactoryAndAuthClient)
            {
                shouldDisposeRequestFactoryAndAuthClient = false;
                CloudCredentialsPair.Dispose(authClient, requestFactory);
            }
        }

        public async ValueTask DisposeAsync()
        {
            GameServices.Dispose();
            await Rooms.DisposeAsync();
            await Worlds.DisposeAsync();

            if (shouldDisposeRequestFactoryAndAuthClient)
            {
                shouldDisposeRequestFactoryAndAuthClient = false;
                await CloudCredentialsPair.DisposeAsync(authClient, requestFactory);
            }
        }
    }
}
