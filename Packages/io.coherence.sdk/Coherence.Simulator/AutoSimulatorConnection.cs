// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Simulator
{
    using Cloud;
    using Coherence.Common;
    using Coherence.Toolkit;
    using Coherence.Runtime;
    using Connection;
    using Coherence.Log;
    using Logger = Log.Logger;
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine;


    [AddComponentMenu("coherence/Simulators/Auto Simulator Connection")]
    [HelpURL("https://docs.coherence.io/v/1.4/manual/simulation-server/client-vs-simulator-logic#connecting-simulators-automatically-to-rs-autosimulatorconnection-component")]
    public class AutoSimulatorConnection : MonoBehaviour
    {
        private readonly Logger logger = Log.GetLogger<AutoSimulatorConnection>();

        public float reconnectTime = 3f;
        private Coroutine reconnectCoroutine;
        private CoherenceBridge bridge;
        private IClient client;
        private bool IsSimulator => SimulatorUtility.IsSimulator || client?.ConnectionType == ConnectionType.Simulator;
        private bool usingWorld => SimulatorUtility.SimulatorType == SimulatorUtility.Type.World;

        private EndpointData endpoint;
        public EndpointData Endpoint => endpoint;

        [Tooltip("Number of connection attempts before trying to resolve the endpoint again. (only applicable for worlds)")]
        public int attemptsBeforeRefetch = 3;

        private ulong worldId;
        private int currentReconnectAttempts = 0;

        private WorldsService worldsService;

        private EndpointData CommandLineRoomEndpoint => endpoint = new EndpointData()
        {
            roomId = (ushort)SimulatorUtility.RoomId,
            uniqueRoomId = SimulatorUtility.UniqueRoomId,
            host = SimulatorUtility.Ip,
            port = SimulatorUtility.Port,
            runtimeKey = RuntimeSettings.Instance.RuntimeKey,
            schemaId = RuntimeSettings.Instance.SchemaID,
            worldId = SimulatorUtility.WorldId,
            authToken = SimulatorUtility.AuthToken,
            region = SimulatorUtility.Region
        };

        private void Start()
        {
            if (!IsSimulator)
            {
                enabled = false;
                return;
            }

            logger.Info($"Auto-connecting coherence Simulator to Endpoint: [{SimulatorUtility.ToString()}]");

            if (CoherenceBridgeStore.TryGetBridge(gameObject.scene, null, this, out bridge))
            {
                client = bridge.Client;
                client.OnConnected += NetworkOnConnected;
                client.OnDisconnected += NetworkOnDisconnected;

                worldId = SimulatorUtility.WorldId;
                endpoint = CommandLineRoomEndpoint;
                if (usingWorld)
                {
                    ResolveWorldEndpoint();
                }
                else
                if (endpoint.Validate().isValid)
                {
                    StartReconnect();
                }
                else
                {
                    logger.Error(Error.SimulatorAutoConnectEndpoint);
                }
            }
            else
            {
                logger.Error(Error.SimulatorAutoConnectBridge);
            }
        }

        private void OnDestroy()
        {
            if (client != null)
            {
                client.OnConnected -= NetworkOnConnected;
                client.OnDisconnected -= NetworkOnDisconnected;
            }
        }

        private void NetworkOnConnected(ClientID _)
        {
            if (IsSimulator)
            {
                logger.Info("Connection successful.");
                currentReconnectAttempts = 0;
            }
        }

        private void NetworkOnDisconnected(ConnectionCloseReason connectionCloseReason)
        {
            if (IsSimulator)
            {
                logger.Error(Error.SimulatorAutoConnectDisconnected,
                    ("reason", connectionCloseReason));
                StartReconnect();
            }
        }

        private void StartReconnect()
        {
            if (reconnectCoroutine != null)
            {
                StopCoroutine(reconnectCoroutine);
            }

            reconnectCoroutine = StartCoroutine(Reconnect());
        }

        private IEnumerator Reconnect()
        {
            logger.Info("Starting reconnect...");

            bool connected = client.IsConnected();
            while (!connected)
            {
                if (client.IsDisconnected())
                {
                    ConnectToEndpoint(endpoint);
                    currentReconnectAttempts++;
                }

                connected = client.IsConnected();

                if (connected)
                {
                    logger.Info("Connected.");
                    currentReconnectAttempts = 0;
                    yield break;
                }

                if (usingWorld && attemptsBeforeRefetch > 0 && currentReconnectAttempts > attemptsBeforeRefetch)
                {
                    yield return new WaitForSeconds(reconnectTime);
                    ResolveWorldEndpoint();
                    yield break;
                }

                yield return new WaitForSeconds(reconnectTime);
                connected = client.IsConnected();
            }
        }

        private void ConnectToEndpoint(EndpointData endpoint)
        {
            logger.Info($"Connecting as simulator to endpoint {endpoint}", ("slug", RuntimeSettings.Instance.SimulatorSlug),
                ("sdkVersion", RuntimeSettings.Instance.VersionInfo.SdkRevisionOrVersion), ("rsVersion", RuntimeSettings.Instance.VersionInfo.Engine));

            var settings = ConnectionSettings.Default;
            settings.UseDebugStreams = RuntimeSettings.Instance.UseDebugStreams;

            // if version is uninitialized, use the one in runtime settings
            if (string.IsNullOrEmpty(endpoint.rsVersion))
            {
                endpoint.rsVersion = RuntimeSettings.Instance.VersionInfo.Engine;
            }

            client.Connect(endpoint, settings, ConnectionType.Simulator);
        }

        private async void ResolveWorldEndpoint()
        {
            logger.Info("Resolving world endpoint...");

            bool fetchSuccessful = false;
            EndpointData newEndpoint;
            if (SimulatorUtility.Region == SimulatorUtility.LocalRegionParameter)
            {
                newEndpoint = new EndpointData
                {
                    host = SimulatorUtility.Ip,
                    port = RuntimeSettings.Instance.IsWebGL ? RuntimeSettings.Instance.LocalWorldWebPort : RuntimeSettings.Instance.LocalWorldUDPPort,
                    worldId = SimulatorUtility.WorldId,
                    runtimeKey = RuntimeSettings.Instance.RuntimeKey,
                    schemaId = RuntimeSettings.Instance.SchemaID,
                    authToken = SimulatorUtility.AuthToken,
                    region = SimulatorUtility.Region
                };
                endpoint = newEndpoint;
                fetchSuccessful = endpoint.Validate().isValid;
            }
            else
            {
                try
                {
                    worldsService = bridge.CloudService.Worlds;

                    while (!worldsService.IsLoggedIn)
                    {
                        await Task.Yield();
                    }

                    var worlds = await worldsService.FetchWorldsAsync();

                    foreach (WorldData worldData in worlds)
                    {
                        if (worldData.WorldId == worldId)
                        {
                            newEndpoint = new EndpointData()
                            {
                                host = worldData.Host.Ip,
                                port = worldData.Host.UDPPort,
                                worldId = worldData.WorldId,
                                runtimeKey = RuntimeSettings.Instance.RuntimeKey,
                                schemaId = RuntimeSettings.Instance.SchemaID,
                                authToken = SimulatorUtility.AuthToken,
                                region = SimulatorUtility.Region
                            };
                            endpoint = newEndpoint;
                            fetchSuccessful = endpoint.Validate().isValid;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(Error.SimulatorAutoConnectWorldIDFailed,
                        $"World fetching failed for worldID: {worldId} - {ex.Message}");
                }
            }

            if (fetchSuccessful)
            {
                logger.Info($"World endpoint resolved [{endpoint.ToString()}].");
                StartReconnect();
            }
            else
            {
                logger.Error(Error.SimulatorAutoConnectWorldIDFailed,
                    $"World fetching failed for worldID: {worldId} - endpoint was invalid");
            }
        }
    }
}
