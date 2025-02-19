// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Toolkit
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Brisk;
    using Cloud;
    using Common;
    using Connection;
    using Core;
    using Entities;
    using Log;
    using PlayerLoop;
    using ProtocolDef;
    using Relay;
    using Stats;
    using Transport;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.SceneManagement;
    using UnityEngine.Serialization;
    using Debug = System.Diagnostics.Debug;
    using Logger = Log.Logger;
    using Ping = Common.Ping;

    /// <summary>
    /// Component used to establish and maintain a connection with the replication server.
    /// </summary>
    /// <remarks>
    /// It is responsible for keeping the <see cref="CoherenceSync"/> entities in sync.
    ///
    /// To get references to <see cref="CoherenceBridge"/> instances, refer to <see cref="CoherenceBridgeStore"/>.
    /// </remarks>
    [AddComponentMenu("coherence/Coherence Bridge")]
    [DefaultExecutionOrder(ScriptExecutionOrder.CoherenceBridge)]
    [NonBindable]
    [HelpURL("https://docs.coherence.io/v/1.4/manual/components/coherence-bridge")]
    public sealed class CoherenceBridge : CoherenceBehaviour, ICoherenceBridge, IAsyncDisposable, IDisposable
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public class EventsToken
        {
            public Action<(bool liveQuerySynced, bool globalQuerySynced)> OnQuerySynced;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Logger Logger { get; private set; }

        /// <summary>
        /// Prefix prepended to the <see cref="UnityEngine.Object.name"/> of <see cref="CoherenceSync"/> entities spawned through the network.
        /// </summary>
        public string NetworkPrefix => networkPrefix;
        [SerializeField]
        private string networkPrefix = "[network] ";

        /// <inheritdoc cref="FloatingOriginManager.WorldPositionMaxRange"/>
        public static float WorldPositionMaxRange => FloatingOriginManager.WorldPositionMaxRange;

        /// <summary>
        /// Accounts for the time required for the packets to travel between the server and the client when calculating the client-server frame drift.
        /// </summary>
        /// <remarks>
        /// This does not modify the <see cref="Coherence.Core.NetworkTime.ClientSimulationFrame" /> directly,
        /// instead it affects the <see cref="Coherence.Core.NetworkTime.NetworkTimeScale" />.
        /// </remarks>
        public bool adjustSimulationFrameByPing;

        [FormerlySerializedAs("globalQueryOn")]
        [SerializeField]
        private bool enableClientConnections = true;

        /// <summary>
        /// Allows for client connection entities to be created.
        /// </summary>
        /// <seealso cref="CoherenceClientConnectionManager" />
        /// <seealso cref="ClientConnectionEntry" />
        /// <seealso cref="SimulatorConnectionEntry" />
        public bool EnableClientConnections
        {
            get => enableClientConnections;
            set => enableClientConnections = value;
        }

        [Obsolete("Access to this member will be removed in a future version. Use the EnableClientConnections property instead.")]
        [Deprecated("07/2024", 1, 2, 4, Reason = "Field was renamed and made private to improve encapsulation. Use the EnableClientConnections property instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool globalQueryOn
        {
            get => enableClientConnections;
            set => enableClientConnections = value;
        }

        [Obsolete("Access to this member will be removed in a future version. Use the EnableClientConnections property instead.")]
        [Deprecated("07/2024", 1, 2, 4, Reason = "Field was renamed and made private to improve encapsulation. Use the EnableClientConnections property instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool GlobalQueryOn
        {
            get => enableClientConnections;
            set => enableClientConnections = value;
        }

        /// <summary>
        /// The <see cref="CoherenceSyncConfig"/> to instantiate when this bridge establishes a <see cref="ConnectionType.Client"/> connection.
        /// </summary>
        /// <seealso cref="EnableClientConnections"/>
        /// <seealso cref="SimulatorConnectionEntry"/>
        public CoherenceSyncConfig ClientConnectionEntry;

        /// <inheritdoc cref="ClientConnectionEntry"/>
        public CoherenceSyncConfig GetClientConnectionEntry() => ClientConnectionEntry;

        /// <summary>
        /// The <see cref="CoherenceSyncConfig"/> to instantiate when this bridge establishes a <see cref="ConnectionType.Simulator"/> connection.
        /// </summary>
        /// <seealso cref="EnableClientConnections"/>
        /// <seealso cref="ClientConnectionEntry"/>
        public CoherenceSyncConfig SimulatorConnectionEntry;

        /// <inheritdoc cref="SimulatorConnectionEntry"/>
        public CoherenceSyncConfig GetSimulatorConnectionEntry() => SimulatorConnectionEntry;

        /// <summary>
        /// <inheritdoc cref="Coherence.Core.NetworkTime.ClientFixedSimulationFrame"/>
        /// </summary>
        public long ClientFixedSimulationFrame => NetworkTime.ClientFixedSimulationFrame;

        /// <summary>
        /// Network time in double precision.
        /// </summary>
        /// <seealso cref="NetworkTime"/>
        public double NetworkTimeAsDouble => NetworkTime?.TimeAsDouble ?? 0f;

        /// <summary>
        /// Whether the bridge has established a connection.
        /// </summary>
        public bool IsConnected => Client?.IsConnected() ?? false;

        /// <summary>
        /// Whether the bridge is establishing a connection.
        /// </summary>
        public bool IsConnecting => Client?.IsConnecting() ?? false;

        /// <summary>
        /// Stats related to inbound and outbound data through the connection.
        /// </summary>
        public Stats NetStats => Client?.Stats;

        /// <summary>
        /// The connection type that was used when connecting to the replication server.
        /// </summary>
        /// <remarks>
        /// Falls back to <see cref="Coherence.Connection.ConnectionType.Client"/> if no connection attempt has yet been made.
        /// </remarks>
        public ConnectionType ConnectionType => Client.ConnectionType;

        /// <summary>
        /// When <see langword="true"/>, this client is considered a host of this session.
        /// </summary>
        /// <remarks>
        /// Any entities using <see cref="CoherenceInput" /> and <see cref="CoherenceSync.SimulationType.ServerSideWithClientInput" />,
        /// will have their state authority transferred to the client that <see cref="IsSimulatorOrHost"/>.
        /// Any entities marked as <see cref="CoherenceSync.SimulationType.ServerSide"/> will remain active on this client.
        /// Always <see langword="true"/> for <see cref="Coherence.Connection.ConnectionType.Simulator" />.
        /// Only the client that created a room can become its host.
        /// </remarks>
        public bool IsSimulatorOrHost => ConnectionType == ConnectionType.Simulator ||
                                         (ConnectionType == ConnectionType.Client && clientAsHost);
        private bool clientAsHost;

        [SerializeField, Tooltip("If enabled, this CoherenceBridge instance will be saved as DontDestroyOnLoad and it will keep its connection alive between networked scene changes." +
                                 " When loading a different Scene with another CoherenceBridge, the secondary Bridge will pass the Scene information to the main one")]
        internal bool mainBridge;

        /// <summary>
        /// Determines if this bridge is the main one.
        /// </summary>
        /// <remarks>
        /// A main bridge (or master bridge) represents the principal connection or scene in the game.
        /// It's set to <see cref="DontDestroyOnLoad"/> and has higher resolution priority.
        ///
        /// Features like scene transitioning (see <see cref="SceneManager"/>) rely on the
        /// idea of a main bridge for orchestrating the transitioning.
        /// </remarks>
        /// <seealso cref="CoherenceBridgeStore.MasterBridge"/>
        public bool IsMain => mainBridge;

        [SerializeField, Tooltip("Uniquely identify the Scene this CoherenceBridge is attached to using the Build Index of the Scene. If this is a secondary Bridge, the identifier will be passed to the main one.")]
        internal bool useBuildIndexAsId;

        [SerializeField, Tooltip("Uniquely identify the Scene this CoherenceBridge is attached to, it will be initialized with the build index of the Scene. If this is a secondary Bridge, the identifier will be passed to the main one.")]
        internal uint sceneIdentifier;

        /// <summary>
        /// Other bridges use this variable to store their identifiers when updating the active networked Scene.
        /// </summary>
        /// <remarks>
        /// Used when <see cref="IsMain"/> is <see langword="true"/>.
        /// </remarks>
        /// <seealso cref="IsMain"/>
        internal uint? overrideSceneId;

#if !ENABLE_INPUT_SYSTEM
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FixedUpdateInput FixedUpdateInput { get; } = new();
#endif

        /// <summary>
        /// <inheritdoc cref="Coherence.Core.NetworkTime.OnFixedNetworkUpdate" />
        /// </summary>
        public event Action OnFixedNetworkUpdate
        {
            add => NetworkTime.OnFixedNetworkUpdate += value;
            remove => NetworkTime.OnFixedNetworkUpdate -= value;
        }

        /// <summary>
        /// <inheritdoc cref="Coherence.Core.NetworkTime.OnLateFixedNetworkUpdate" />
        /// </summary>
        public event Action OnLateFixedNetworkUpdate
        {
            add => NetworkTime.OnLateFixedNetworkUpdate += value;
            remove => NetworkTime.OnLateFixedNetworkUpdate -= value;
        }

        /// <summary>
        /// <inheritdoc cref="Coherence.Core.NetworkTime.OnTimeReset" />
        /// </summary>
        public event Action OnTimeReset
        {
            add => NetworkTime.OnTimeReset += value;
            remove => NetworkTime.OnTimeReset -= value;
        }

        /// <summary>
        /// Called when a network entity has been created.
        /// </summary>
        /// <remarks>
        /// Called before instantiation of the <see cref="CoherenceSync"/> instance.
        /// </remarks>
        public UnityEvent<CoherenceBridge, NetworkEntityState> onNetworkEntityCreated = new();

        /// <summary>
        /// Called when a network entity has been destroyed.
        /// </summary>
        /// <remarks>
        /// Called before destruction of the <see cref="CoherenceSync"/> instance.
        /// </remarks>
        public UnityEvent<CoherenceBridge, NetworkEntityState, DestroyReason> onNetworkEntityDestroyed = new();

        /// <summary>
        /// Enables automatic client-server synchronization.
        /// </summary>
        /// <remarks>
        /// Can be disabled temporarily for bullet time effects to intentionally de-synchronize clients for a short while.
        /// When set to <see langword="true"/>, <see cref="Time.timeScale"/> is nudged up/down so the game speed adapts to synchronize the game clock with the server clock.
        /// </remarks>
        public bool controlTimeScale = true;

        /// <summary>
        /// Unique ID of this connection as assigned by the server.
        /// </summary>
        /// <remarks>
        /// If a <see cref="ClientConnectionEntry" /> has been assigned, this value can also be accessed using <see cref="CoherenceClientConnection.ClientId"/>.
        /// </remarks>
        public ClientID ClientID => Client.ClientID;

        /// <summary>
        /// Holds information about all <see cref="CoherenceClientConnection"/>s in this session.
        /// </summary>
        /// <remarks>
        /// Can be used to send messages between clients if you're using Connection Prefabs.
        /// </remarks>
        public CoherenceClientConnectionManager ClientConnections { get; private set; }

        /// <summary>
        /// Handles client entities with server-side input.
        /// </summary>
        public CoherenceInputManager InputManager { get; private set; }

        /// <summary>
        /// Handles authority requests over entities and adoptions of orphaned entities.
        /// </summary>
        public AuthorityManager AuthorityManager { get; private set; }

        /// <summary>
        /// Holds information about all entities that are visible to this connection.
        /// </summary>
        public EntitiesManager EntitiesManager { get; private set; }

        /// <summary>
        /// Holds information about all unique entities in this session.
        /// </summary>
        /// <remarks>
        /// Unique entities are created by setting <see cref="CoherenceSync.uniquenessType"/> to
        /// <see cref="CoherenceSync.UniquenessType.NoDuplicates"/>.
        /// </remarks>
        public UniquenessManager UniquenessManager { get; private set; }

        /// <summary>
        /// Controls what scene this client should receive and send updates for.
        /// </summary>
        public CoherenceSceneManager SceneManager { get; set; }

        /// <summary>
        /// Manipulates the Floating Origin.
        /// </summary>
        public FloatingOriginManager FloatingOriginManager { get; private set; }

        /// <summary>
        /// Service to communicate with the coherence Cloud.
        /// </summary>
        /// <remarks>
        /// You must be logged in, with an Organization and Project set, to be able to use it.
        /// Check the coherence Cloud tab in the coherence Hub window for more details.
        /// </remarks>
        public CloudService CloudService { get; private set; }

        /// <summary>
        /// Provides access to the network synchronized time and frame state.
        /// </summary>
        public INetworkTime NetworkTime => Client?.NetworkTime ?? null;

        /// <summary>
        /// Reference to the lower-level client that handles the connection.
        /// </summary>
        public IClient Client { get; private set; }

        /// <summary>
        /// Reference to the global query entity.
        /// </summary>
        public GameObject GlobalQueryGameObject { get; set; }

        /// <summary>
        /// Reference to the scene where this bridge is.
        /// </summary>
        public Scene Scene => gameObject.scene;

        private EventsToken events;

        private Scene? instantiationScene;
        /// <summary>
        /// Scene where this Bridge will instantiate remote entities.
        /// </summary>
        /// <remarks>
        /// Setting this value will register the bridge to its current scene.
        /// </remarks>
        public Scene? InstantiationScene
        {
            get
            {
                return instantiationScene ?? Scene;
            }
            set
            {
                CoherenceBridgeStore.DeregisterBridge(this);
                instantiationScene = value;
                if (value.HasValue)
                {
                    CoherenceBridgeStore.RegisterBridge(this, value.Value, mainBridge);
                }
            }
        }

        /// <inheritdoc cref="FloatingOriginManager.OnFloatingOriginShifted"/>
        public event Action<FloatingOriginShiftArgs> OnFloatingOriginShifted
        {
            add
            {
                if (FloatingOriginManager is not null)
                {
                    FloatingOriginManager.OnFloatingOriginShifted += value;
                }
            }
            remove
            {
                if (FloatingOriginManager is not null)
                {
                    FloatingOriginManager.OnFloatingOriginShifted = value;
                }
            }
        }

        /// <inheritdoc cref="FloatingOriginManager.OnAfterFloatingOriginShifted"/>
        public event Action<FloatingOriginShiftArgs> OnAfterFloatingOriginShifted
        {
            add
            {
                if (FloatingOriginManager is not null)
                {
                    FloatingOriginManager.OnAfterFloatingOriginShifted += value;
                }
            }
            remove
            {
                if (FloatingOriginManager is not null)
                {
                    FloatingOriginManager.OnAfterFloatingOriginShifted = value;
                }
            }
        }

        /// <summary>
        /// Number of entities visible by this bridge.
        /// </summary>
        public int EntityCount => EntitiesManager.EntityCount;

        /// <summary>
        /// Latency related information.
        /// </summary>
        public Ping Ping => Client.Ping;

        /// <summary>
        /// Invoked when the bridge has successfully connected.
        /// </summary>
        public UnityEvent<CoherenceBridge> onConnected = new();

        // TODO internalize
        [EditorBrowsable(EditorBrowsableState.Never)]
        public event Action<ICoherenceBridge> OnConnectedInternal;

        /// <summary>
        /// Called when the bridge has disconnected.
        /// </summary>
        public UnityEvent<CoherenceBridge, ConnectionCloseReason> onDisconnected = new();

        /// <summary>
        /// Called when the bridge experiences a connection error.
        /// </summary>
        public UnityEvent<CoherenceBridge, ConnectionException> onConnectionError = new();

        /// <summary>Called when all entities that originated from a <see cref="CoherenceLiveQuery" /> have been synced.</summary>
        /// <remarks>
        /// This event does not guarantee that all entities will be in the exact state as of the
        /// <see cref="CoherenceLiveQuery" /> creation, it does however guarantee that the entities that were present at the
        /// time of query creation have been synced with this client. Eventual consistency rules still apply.
        /// </remarks>
        public UnityEvent<CoherenceBridge> onLiveQuerySynced = new();

        /// <summary>
        /// The transform of this entity.
        /// </summary>
        public Transform Transform => transform;

        [SerializeField]
        private string uniqueId;

        [SerializeField]
        private bool autoLoginAsGuest = true;

        /// <summary>
        /// Set to <see langword="true"/> to authenticate as a guest.
        /// </summary>
        public bool AutoLoginAsGuest => autoLoginAsGuest;

        internal string UniqueId => uniqueId;

        /// <inheritdoc cref="CoherenceRelayManager"/>
        private CoherenceRelayManager relayManager;

        private bool shouldDisposeCloudService;

        /// <summary>
        /// Attempts a connection to the replication server.
        /// </summary>
        /// <remarks>
        /// The connection type will be determined by <see cref="SimulatorUtility.IsSimulator"/>.
        /// </remarks>
        /// <param name="endpoint">The address of the replication server to connect to.</param>
        /// <param name="connectionSettings">The settings to be used for this connection. If null, <see cref="ConnectionSettings.Default"/> will be used.</param>
        public void Connect(EndpointData endpoint, ConnectionSettings connectionSettings = null)
        {
            Connect(endpoint, SimulatorUtility.IsSimulator ? ConnectionType.Simulator : ConnectionType.Client, false, connectionSettings);
        }

        /// <summary>
        /// Attempts a connection to the replication server.
        /// </summary>
        /// <param name="endpoint">The address of the replication server to connect to.</param>
        /// <param name="connectionType">Used to specify if you are connecting as a client or simulator.</param>
        /// <param name="connectionSettings">The settings to be used for this connection. If null, <see cref="ConnectionSettings.Default"/> will be used.</param>
        public void Connect(EndpointData endpoint, ConnectionType connectionType, ConnectionSettings connectionSettings = null)
        {
            Connect(endpoint, connectionType, false, connectionSettings);
        }

        /// <summary>
        /// Attempts to connect as a host-client.
        /// </summary>
        /// <remarks>
        /// Connected client will become a host of the room (see <see cref="IsSimulatorOrHost" />).
        /// </remarks>
        /// <param name="endpoint">The address of the replication server to connect to.</param>
        /// <param name="connectionSettings">The settings to be used for this connection. If null, <see cref="ConnectionSettings.Default" /> will be used.</param>
        public void ConnectAsHost(EndpointData endpoint, ConnectionSettings connectionSettings = null)
        {
            if (string.IsNullOrEmpty(endpoint.roomSecret))
            {
                throw new ArgumentException(
                    $"Client cannot become a host because a room secret is missing from the {nameof(EndpointData)}. " +
                    $"Make sure to pass a room secret returned after room creation ({nameof(RoomData)}.{nameof(RoomData.Secret)}).",
                    nameof(endpoint));
            }

            Connect(endpoint, ConnectionType.Client, true, connectionSettings);
        }

        /// <summary>
        /// Attempts to reconnect to the replication server.
        /// </summary>
        /// <remarks>
        /// Endpoint, connection settings and connection type used with the previous connection attempt will be used again.
        /// </remarks>
        public void Reconnect()
        {
            Client.Reconnect();
        }

        /// <summary>
        /// Disconnects from the replication server.
        /// </summary>
        /// <remarks>
        /// When not connected, it does nothing.
        /// </remarks>
        /// <see cref="IsConnected"/>
        public void Disconnect()
        {
            Client.Disconnect();
        }

        /// <summary>
        /// Attempts to join a room.
        /// </summary>
        /// <param name="room">The room settings.</param>
        public void JoinRoom(RoomData room)
        {
            var (endpoint, valid, validationErrorMessage) = RoomData.GetRoomEndpointData(room);
            if (valid)
            {
                Connect(endpoint);
            }
            else
            {
                Logger.Error(Error.ToolkitBridgeInvalidEndpoint,
                    $"Invalid {nameof(EndpointData)}: {validationErrorMessage}");
            }
        }

        /// <summary>
        /// Attempts to connect to a world.
        /// </summary>
        /// <param name="world">The world settings.</param>
        public void JoinWorld(WorldData world)
        {
            var (endpoint, valid, validationErrorMessage) = WorldData.GetWorldEndpoint(world);
            if (valid)
            {
                Connect(endpoint);
            }
            else
            {
                Logger.Error(Error.ToolkitBridgeInvalidEndpoint,
                    $"Invalid {nameof(EndpointData)}: {validationErrorMessage}");
            }
        }

        /// <summary>
        /// Get an entity known by this bridge.
        /// </summary>
        /// <param name="id">An existing entity (known by this bridge).</param>
        public ICoherenceSync GetCoherenceSyncForEntity(Entity id)
        {
            return EntitiesManager.GetCoherenceSyncForEntity(id);
        }

        /// <summary>
        /// Get the state of an entity known by this bridge.
        /// </summary>
        /// <param name="id">A *valid* Entity (meaning it exists in the mapper)</param>
        public NetworkEntityState GetNetworkEntityStateForEntity(Entity id)
        {
            return EntitiesManager.GetNetworkEntityStateForEntity(id);
        }

        /// <summary>
        /// Get the <see cref="Coherence.Entities.Entity"/> for a <see cref="GameObject"/> with <see cref="CoherenceSync"/>.
        /// </summary>
        public Entity UnityObjectToEntityId(GameObject from)
        {
            return EntitiesManager.UnityObjectToEntityId(from);
        }

        /// <summary>
        /// Get the <see cref="Coherence.Entities.Entity"/> for a <see cref="Transform"/> with <see cref="CoherenceSync"/>.
        /// </summary>
        public Entity UnityObjectToEntityId(Transform from)
        {
            return EntitiesManager.UnityObjectToEntityId(from);
        }

        /// <summary>
        /// Get the <see cref="Coherence.Entities.Entity"/> for a <see cref="CoherenceSync"/>.
        /// </summary>
        public Entity UnityObjectToEntityId(ICoherenceSync from)
        {
            return EntitiesManager.UnityObjectToEntityId(from);
        }

        /// <summary>
        /// Get the <see cref="GameObject"/> for an <see cref="Coherence.Entities.Entity"/>.
        /// </summary>
        public GameObject EntityIdToGameObject(Entity from)
        {
            return EntitiesManager.EntityIdToGameObject(from);
        }

        /// <summary>
        ///     Get the <see cref="Transform"/> for an <see cref="Coherence.Entities.Entity"/>.
        /// </summary>
        public Transform EntityIdToTransform(Entity from)
        {
            return EntitiesManager.EntityIdToTransform(from);
        }

        /// <summary>
        /// Get the <see cref="RectTransform"/> for an <see cref="Coherence.Entities.Entity"/>.
        /// </summary>
        public RectTransform EntityIdToRectTransform(Entity from)
        {
            return EntitiesManager.EntityIdToRectTransform(from);
        }

        /// <summary>
        /// Get the <see cref="CoherenceSync" /> for an <see cref="Coherence.Entities.Entity" />.
        /// </summary>
        public CoherenceSync EntityIdToCoherenceSync(Entity from)
        {
            return EntitiesManager.EntityIdToCoherenceSync(from) as CoherenceSync;
        }

        // TODO internalize
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnNetworkEntityDestroyedInvoke(NetworkEntityState state, DestroyReason destroyReason)
        {
            try
            {
                onNetworkEntityDestroyed?.Invoke(this, state, destroyReason);
            }
            catch (Exception handlerException)
            {
                Logger.Error(Error.ToolkitBridgeCallbackHandlerException,
                    ("callback", nameof(onNetworkEntityDestroyed)),
                    ("exception", handlerException));
            }
        }

        // TODO internalize
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnNetworkEntityCreatedInvoke(NetworkEntityState state)
        {
            try
            {
                onNetworkEntityCreated?.Invoke(this, state);
            }
            catch (Exception handlerException)
            {
                Logger.Error(Error.ToolkitBridgeCallbackHandlerException,
                    ("callback", nameof(onNetworkEntityCreated)),
                    ("exception", handlerException));
            }
        }

#if COHERENCE_FEATURE_NATIVE_CORE
        private IClient InstantiateNativeCoreClient(TransportType transportType)
        {
            Logger.Info("Using Native Core");

            var client = NativeCoreFactory.CreateClient(
                Impl.GetDataInteropHandler(),
                RuntimeSettings.Instance.CombinedSchemaText,
                Logger);

#if UNITY_WEBGL && !UNITY_EDITOR
            // To use WebTransport
            client.SetTransportFactory(new DefaultTransportFactory());
#else
            client.SetTransportType(transportType, RuntimeSettings.Instance.TransportConfiguration);
#endif

            return client;
        }
#endif

        private IClient InstantiateClient()
        {
            var transportType = RuntimeSettings.Instance.TransportType;
            if (SimulatorUtility.IsCloudSimulator)
            {
                transportType = SimulatorUtility.EnsureCorrectCloudSimulatorTransport(Logger, transportType);
            }

#if COHERENCE_FEATURE_NATIVE_CORE
            if (RuntimeSettings.Instance.UseNativeCore)
            {
                return InstantiateNativeCoreClient(transportType);
            }
#endif

            var briskServices = BriskServices.Default;
            briskServices.KeepAliveProvider = () => !RuntimeSettings.Instance.DisableKeepAlive;

#if COHERENCE_HAS_RSL
            if (transportType != TransportType.UDPOnly)
            {
                Logger.Info($"RSL is enabled, forcing {nameof(TransportType.UDPOnly)} transport type",
                    ("originalTransport", transportType));
                transportType = TransportType.UDPOnly;
            }
#endif

            return Core.GetNewClient(
                Impl.GetRootDefinition(),
                Logger,
                null,
                transportType,
                RuntimeSettings.Instance.TransportConfiguration,
                briskServices
            );
        }

        internal void InitializeClient()
        {
            if (Client == null)
            {
                if (Impl.GetRootDefinition == null || Impl.GetDataInteropHandler == null)
                {
                    Logger.Error(Error.ToolkitBridgeCodeNotBaked);
                    return;
                }

                Client = InstantiateClient();
                Logger.Debug($"{nameof(IClient)} (Core) implementation: {Client.GetType()}");

                Client.OnConnected += HandleConnected;
                Client.OnConnectionError += HandleConnectionError;
                Client.OnCommand += OnCommand;
                Client.OnInput += OnInput;
                Client.NetworkTime.FixedTimeStep = Time.fixedDeltaTime;
                Client.NetworkTime.MaximumDeltaTime = Time.maximumDeltaTime;
#if !ENABLE_INPUT_SYSTEM
                Client.NetworkTime.OnFixedNetworkUpdate += () => FixedUpdateInput.FixedUpdate();
#endif
                InputManager = new CoherenceInputManager(this);
                AuthorityManager = new AuthorityManager(Client, this);
                EntitiesManager = new EntitiesManager(this, ClientConnections, InputManager, UniquenessManager, Impl.GetRootDefinition(), Logger);
                FloatingOriginManager = new FloatingOriginManager(Client, EntitiesManager, Logger);
                Client.OnDisconnected += HandleDisconnected;

                if (EnableClientConnections)
                {
                    SetInitialScene(ResolveSceneId());
                }
                else
                {
                    Client.InitialScene = 0;
                }

                CoherenceLoop.AddBridge(this);
            }
        }

        /// <summary>
        /// Set the initial scene to connect to.
        /// </summary>
        /// <remarks>
        /// Setting this value after connecting has no immediate effect, but
        /// will affect subsequent reconnect attempts.
        /// To set the scene after connecting, use <inheritdoc cref="CoherenceSceneManager"/>.
        /// </remarks>
        public void SetInitialScene(uint initialScene)
        {
            Client.InitialScene = initialScene;
        }

        private void Connect(EndpointData endpoint, ConnectionType connectionType, bool asHost, ConnectionSettings connectionSettings)
        {
            if (Client == null)
            {
                Logger.Warning(Warning.ToolkitBridgeNotInitialized);
                return;
            }

            if (SimulatorUtility.IsSimulator)
            {
                Logger.Info($"Connecting as simulator to endpoint {endpoint}", ("slug", RuntimeSettings.Instance.SimulatorSlug),
                    ("sdkVersion", RuntimeSettings.Instance.VersionInfo.SdkRevisionOrVersion), ("rsVersion", RuntimeSettings.Instance.VersionInfo.Engine));
            }

            // if version is uninitialized, use the one in runtime settings
            if (string.IsNullOrEmpty(endpoint.rsVersion))
            {
                endpoint.rsVersion = RuntimeSettings.Instance.VersionInfo.Engine;
            }

            if (connectionSettings == null)
            {
                connectionSettings = ConnectionSettings.Default;
                connectionSettings.UseDebugStreams = RuntimeSettings.Instance.UseDebugStreams;
            }

            clientAsHost = asHost;
            Client.Connect(endpoint, connectionSettings, connectionType);
        }

        // for components, we don't expose direct creation of instances - add as component instead
        private CoherenceBridge()
        {

        }

        private void Awake()
        {
            if (LinkToMasterBridge())
            {
                return;
            }

#if DEBUG || UNITY_INCLUDE_TESTS
            if (IDisposableInternal.CurrentInitializationContext is not { Length: > 0 })
            {
                IDisposableInternal.SetCurrentInitializationContext(gameObject.scene.name + ".CoherenceBridge");
            }
#endif

            Logger = Log.GetLogger<CoherenceBridge>(gameObject.scene)
                .WithArgs(("context", this));
            events = new EventsToken { OnQuerySynced = OnQuerySynced };

            ClientConnections = new CoherenceClientConnectionManager(this, Logger);
            UniquenessManager = new UniquenessManager(Logger);
            CloudService = GetOrCreateCloudService();
            CloudService.Rooms.LobbyService.OnPlaySessionStartedInternal += JoinRoom;
            relayManager = new CoherenceRelayManager(Logger);

            CoherenceBridgeStore.RegisterBridge(this, gameObject.scene.handle, mainBridge);

            if (mainBridge && EnableClientConnections)
            {
                if (gameObject.transform.parent != null)
                {
                    Logger.Error(Error.ToolkitBridgeMasterNotRoot);
                }
                else
                {
                    DontDestroyOnLoad();
                }
            }

            InitializeClient();

            SceneManager = new CoherenceSceneManager(ClientConnections, Client);
            ClientConnections.OnMyClientConnection += SceneManager.GotMyClientConnection;

            CloudService GetOrCreateCloudService()
            {
#if UNITY
                if (SimulatorUtility.UseSharedCloudCredentials)
                {
                    return CloudService.ForSimulator();
                }
#endif
                shouldDisposeCloudService = true;
                return CloudService.ForClient(uniqueId: uniqueId, autoLoginAsGuest: autoLoginAsGuest);
            }
        }

        private bool LinkToMasterBridge()
        {
            if (CoherenceBridgeStore.MasterBridge == null)
            {
                return false;
            }

            CoherenceBridgeStore.MasterBridge.instantiationScene = gameObject.scene;

            CoherenceBridgeStore.MasterBridge.overrideSceneId = ResolveSceneId();

            if (CoherenceBridgeStore.MasterBridge.IsConnected)
            {
                CoherenceBridgeStore.MasterBridge.SceneManager.SetClientScene(ResolveSceneId());
            }
            else
            {
                CoherenceBridgeStore.MasterBridge.SetInitialScene(ResolveSceneId());
            }

            Destroy(this);
            return true;
        }

        internal uint ResolveSceneId()
        {
            if (overrideSceneId.HasValue)
            {
                return overrideSceneId.Value;
            }

            var buildIndex = gameObject.scene.buildIndex;

            return useBuildIndexAsId && buildIndex >= 0 ? (uint)buildIndex : sceneIdentifier;
        }

        /// <summary>
        /// Cleanup of the bridge before destroying it.
        /// </summary>
        /// <remarks>
        /// The bridge is disposed automatically, there shouldn't be a reason for manual disposal.
        /// </remarks>
        void IDisposable.Dispose()
        {
            CoherenceLoop.RemoveBridge(this);
            Client?.Dispose();
            if (CloudService != null)
            {
                CloudService.Rooms.LobbyService.OnPlaySessionStartedInternal -= JoinRoom;
                if (shouldDisposeCloudService)
                {
                    CloudService.Dispose();
                }

                CloudService = null;
            }

            CoherenceBridgeStore.DeregisterBridge(gameObject.scene.handle);
            ((IDisposable)UniquenessManager)?.Dispose();

            Logger?.Dispose();
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            shouldDisposeCloudService = true;
            CoherenceLoop.RemoveBridge(this);
            Client?.Dispose();

            CoherenceBridgeStore.DeregisterBridge(gameObject.scene.handle);
            if (CloudService != null)
            {
                CloudService.Rooms.LobbyService.OnPlaySessionStartedInternal -= JoinRoom;
                if (shouldDisposeCloudService)
                {
                    await CloudService.DisposeAsync();
                }

                CloudService = null;
            }

            Logger?.Dispose();
        }

        private void Start() => CoherenceSyncConfigRegistry.Instance.WarmUp(this);
        private void OnDestroy() => ((IDisposable)this).Dispose();

#if UNITY_EDITOR
        private bool runInBackgroundWarningShown;
        private void OnApplicationFocus(bool hasFocus)
        {
            if (IsConnected && !runInBackgroundWarningShown && !hasFocus && !Application.runInBackground)
            {
                runInBackgroundWarningShown = true;
                Logger.Warning(Warning.ToolkitBridgeEnableRunInBackground,
                    $"The application has lost focus and {nameof(Application)}.{nameof(Application.runInBackground)} is " +
                    "not set. Network messages won't be processed correctly. Turn on " +
                    $"{nameof(Application)}.{nameof(Application.runInBackground)} in Edit > Project Settings > " +
                    "Player > Resolution and Presentation, 'Run in Background'");
            }
        }
#endif

        internal void ReceiveFromNetwork()
        {
            ReceiveFromNetworkAndUpdateTime();
            Profiling.Counters.EntityCount.Sample(EntitiesManager.EntityCount);
            SampleNetworkMetrics();
        }

        internal void Interpolate(CoherenceSync.InterpolationLoop interpolationLoop)
        {
            try
            {
                EntitiesManager.InterpolateBindings(interpolationLoop);
            }
            catch (Exception e)
            {
                HandleException(nameof(Interpolate), e);
            }
        }

        internal void InvokeCallbacks(CoherenceSync.InterpolationLoop interpolationLoop)
        {
            try
            {
                EntitiesManager.InvokeCallbacks(interpolationLoop);
            }
            catch (Exception e)
            {
                HandleException(nameof(InvokeCallbacks), e);
            }
        }

        internal void Sample(CoherenceSync.InterpolationLoop interpolationLoop)
        {
            try
            {
                EntitiesManager.SampleBindings(interpolationLoop);
            }
            catch (Exception e)
            {
                HandleException(nameof(Sample), e);
            }
        }

        internal void SyncAndSend()
        {
            try
            {
                EntitiesManager.SyncAndSend();
            }
            catch (Exception e)
            {
                HandleException(nameof(SyncAndSend), e);
            }

            Client.UpdateSending();
            Client.Stats.Flush(Time.frameCount, Time.deltaTime);
        }

        [Conditional("ENABLE_PROFILER")]
        private void SampleNetworkMetrics()
        {
            Profiling.Counters.Latency.Sample(Client.Ping.AverageLatencyMs * 1000000);
            var inStats = Client.Stats.FetchSimpleInStats();
            Profiling.Counters.BandwidthReceived.Value = inStats.OctetTotalSize;
            Profiling.Counters.PacketReceived.Value = inStats.PacketCount;
            Profiling.Counters.MessagesReceived.Value = inStats.MessageCount;
            Profiling.Counters.UpdatesReceived.Value = inStats.ChangeCount;
            Profiling.Counters.CommandsReceived.Value = inStats.CommandCount;
            Profiling.Counters.InputsReceived.Value = inStats.InputCount;

            var outStats = Client.Stats.FetchSimpleOutStats();
            Profiling.Counters.BandwidthSent.Value = outStats.OctetTotalSize;
            Profiling.Counters.PacketsSent.Value = outStats.PacketCount;
            Profiling.Counters.MessagesSent.Value = outStats.MessageCount;
            Profiling.Counters.UpdatesSent.Value = outStats.ChangeCount;
            Profiling.Counters.CommandsSent.Value = outStats.CommandCount;
            Profiling.Counters.InputsSent.Value = outStats.InputCount;
        }



        /// <summary>
        /// Handles <see cref="NetworkTime" /> update and data receiving through <see cref="IClient.UpdateReceiving" />.
        /// </summary>
        private void ReceiveFromNetworkAndUpdateTime()
        {
            if (Client == null || Client.ConnectionState == ConnectionState.Disconnected)
            {
                return;
            }

            Client.NetworkTime.MultiClientMode = !controlTimeScale;
            if (controlTimeScale)
            {
                Client.NetworkTime.Step(Time.timeAsDouble);
                Time.timeScale = Client.NetworkTime.NetworkTimeScale;
            }
            else
            {
                Client.NetworkTime.Step(Time.unscaledTimeAsDouble);
            }

#if !ENABLE_INPUT_SYSTEM
            FixedUpdateInput.Update();
#endif
            Client.UpdateReceiving();

            InputManager.Update();
            relayManager.Update();

            Client.NetworkTime.AccountForPing = adjustSimulationFrameByPing;
        }

        private void OnCommand(IEntityCommand command, MessageTarget target, Entity receiver)
        {
            if (receiver != Entity.InvalidRelative && receiver == EntitiesManager.ConnectionEntityID)
            {
                if (Impl.ReceiveInternalCommand(events, command, Logger))
                {
                    return;
                }
            }

            if (EntitiesManager.TryGetNetworkEntityState(receiver, out NetworkEntityState entity))
            {
                entity.Sync.ReceiveCommand(command, target);
                return;
            }

            if (EntitiesManager.AddDelayedCommand(command, target, receiver))
            {
                return;
            }

            Logger.Error(Error.ToolkitBridgeCanNotHandleCommand,
                ("entity", receiver),
                ("commandType", command.GetType()));
        }

        private void OnInput(IEntityInput input, long inputFrame, Entity receiver)
        {
            if (EntitiesManager.TryGetNetworkEntityState(receiver, out NetworkEntityState entity))
            {
                entity.Sync.Input.HandleInputReceived(input, inputFrame);
            }
            else
            {
                Logger.Error(Error.ToolkitBridgeCanNotHandleInput,
                    ("entity", receiver),
                    ("inputType", input.GetType()));
            }
        }

        private void OnApplicationQuit()
        {
            ((IDisposable)this).Dispose();
            CoherenceSyncConfigRegistry.Instance.CleanUp();
        }

        private void HandleConnected(ClientID clientID)
        {
            OnConnectedInternal?.Invoke(this);
            EntitiesManager.InitializeGlobalQuery();
            relayManager.Open(Client.LastEndpointData);

            Logger.Debug($"Connected with transport: {Client.DebugGetTransportDescription()}");

            try
            {
                onConnected?.Invoke(this);
            }
            catch (Exception e)
            {
                HandleException(nameof(OnApplicationQuit), e);
            }
        }

        private void HandleConnectionError(ConnectionException connectionException)
        {
            try
            {
                onConnectionError?.Invoke(this, connectionException);
            }
            catch (Exception e)
            {
                HandleException(nameof(HandleConnectionError), e);
            }
        }

        private void HandleDisconnected(ConnectionCloseReason closeReason)
        {
            if (GlobalQueryGameObject != null)
            {
                GlobalQueryGameObject.GetComponent<ICoherenceSync>().HandleNetworkedDestruction(false);
                GlobalQueryGameObject = null;
            }

            InputManager.Reset();
            ClientConnections.CleanUp();
            relayManager.Close();

            try
            {
                onDisconnected?.Invoke(this, closeReason);
            }
            catch (Exception e)
            {
                HandleException(nameof(HandleDisconnected), e);
            }
        }

        private void OnQuerySynced((bool liveQuerySynced, bool globalQuerySynced) queryInfo)
        {
            Logger.Debug(nameof(OnQuerySynced), ("liveQuerySynced", queryInfo.liveQuerySynced), ("globalQuerySynced", queryInfo.globalQuerySynced));

            if (queryInfo.liveQuerySynced)
            {
                try
                {
                    onLiveQuerySynced?.Invoke(this);
                }
                catch (Exception e)
                {
                    HandleException(nameof(OnQuerySynced), e);
                }
            }

            if (!EnableClientConnections || !queryInfo.globalQuerySynced)
            {
                return;
            }

            ClientConnections.HandleGlobalQuerySynced();
        }

        /// <inheritdoc cref="FloatingOriginManager.TranslateFloatingOrigin(Vector3d)"/>
        public bool TranslateFloatingOrigin(Vector3d translation)
        {
            return FloatingOriginManager.TranslateFloatingOrigin(translation);
        }

        /// <inheritdoc cref="FloatingOriginManager.TranslateFloatingOrigin(Vector3)"/>
        public bool TranslateFloatingOrigin(Vector3 translation)
        {
            return FloatingOriginManager.TranslateFloatingOrigin(translation);
        }

        /// <inheritdoc cref="FloatingOriginManager.SetFloatingOrigin(Vector3d)"/>
        public bool SetFloatingOrigin(Vector3d newOrigin)
        {
            return FloatingOriginManager.SetFloatingOrigin(newOrigin);
        }

        /// <inheritdoc cref="FloatingOriginManager.GetFloatingOrigin()"/>
        public Vector3d GetFloatingOrigin()
        {
            return FloatingOriginManager.GetFloatingOrigin();
        }

        /// <summary>
        /// Convenience method for tagging this bridge as <see cref="DontDestroyOnLoad"/>.
        /// </summary>
        /// <remarks>
        /// It also sets the <see cref="InstantiationScene"/> to the current scene beforehand,
        /// so that the original scene this entity was present on is preserved.
        /// </remarks>
        /// <seealso cref="InstantiationScene"/>
        public void DontDestroyOnLoad()
        {
            InstantiationScene = gameObject.scene;
            GameObject.DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Sets the type of the transport which will be used when connecting to the replication server.
        /// </summary>
        /// <param name="transportType">The type of transport that will be used.</param>
        /// <param name="transportConfiguration">Configuration of the transport,
        /// specifying what implementation and options will the built-in transports use.</param>
        public void SetTransportType(TransportType transportType, TransportConfiguration transportConfiguration)
        {
            Client.SetTransportType(transportType, transportConfiguration);
        }

        /// <summary>
        /// Sets the transport factory used when connecting to the replication server.
        /// </summary>
        /// <remarks>
        /// A custom transport factory can be used to tunnel traffic via relay.
        /// </remarks>
        /// <param name="transportFactory">An instance of a transport factory.</param>
        public void SetTransportFactory(ITransportFactory transportFactory)
        {
            Client.SetTransportFactory(transportFactory);
        }

        /// <summary>Sets a <see cref="IRelay" /> to be managed.</summary>
        /// <remarks>
        /// The relay must be set before connecting using a <see cref="CoherenceBridge" />.
        /// Setting relay during an active connection will keep it disabled and enable it only on the subsequent connection.
        /// </remarks>
        public void SetRelay(IRelay newRelay)
        {
            relayManager.SetRelay(newRelay);
        }

        private void HandleException(string function, Exception exception)
        {
            Logger.Error(Error.ToolkitBridgeException,
                ("function", function),
                ("exception", exception));
        }
    }
}
