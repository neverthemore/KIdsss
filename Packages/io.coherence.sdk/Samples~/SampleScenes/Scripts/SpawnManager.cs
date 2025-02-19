namespace Coherence.Samples.PlayerSpawner
{
    using UnityEngine;
    using Connection;
    using Toolkit;
    using System.Collections.Generic;

    public class SpawnManager : MonoBehaviour
    {
        [SerializeField]
        private List<Transform> spawnPoints;
        
        [SerializeField]
        private CoherenceSync playerPrefab;

        private CoherenceBridge coherenceBridge;
        private CoherenceSync coherenceSync;
        
        private Queue<Transform> unusedSpawnPoints;
        private HashSet<ClientID> assignedClients;

        private void Awake()
        {
            CoherenceBridgeStore.TryGetBridge(gameObject.scene, out coherenceBridge);
            coherenceSync = GetComponent<CoherenceSync>();
        }
        
        private void OnEnable()
        {
            coherenceBridge.onLiveQuerySynced.AddListener(OnLiveQuery);
            coherenceBridge.onDisconnected.AddListener(OnDisconnected);
        }
        
        private void OnDisable()
        {
            coherenceBridge.onLiveQuerySynced.RemoveListener(OnLiveQuery);
            coherenceBridge.onDisconnected.RemoveListener(OnDisconnected);
        }

        private void OnLiveQuery(CoherenceBridge _)
        {
            if (!coherenceSync.HasStateAuthority)
            {
                return;
            }
            
            unusedSpawnPoints = new Queue<Transform>(spawnPoints);
            assignedClients = new HashSet<ClientID>();

            AssignSpawnPointToClient(coherenceBridge.ClientConnections.GetMine()); // For this Client
            coherenceBridge.ClientConnections.OnCreated += AssignSpawnPointToClient; // For future Clients
        }

        private void AssignSpawnPointToClient(CoherenceClientConnection clientConnection)
        {
            ClientID clientID = clientConnection.ClientId;

            bool hasIndexAssigned = assignedClients.Contains(clientID);
            if (hasIndexAssigned)
            {
                return;
            }

            Vector3 spawnPosition = GetSpawnPointPosition();
            clientConnection.SendClientMessage<Client>(nameof(Client.SpawnPlayer), MessageTarget.AuthorityOnly, spawnPosition);

            assignedClients.Add(clientID);
        }

        private Vector3 GetSpawnPointPosition()
        {
            if (unusedSpawnPoints.Count == 0)
            {
                Debug.LogWarning($"No more spawn points available! Assigning {Vector3.zero}");
                return Vector3.zero;
            }

            Transform spawnPoint = unusedSpawnPoints.Dequeue();
            return spawnPoint.transform.position;
        }
        
        public CoherenceSync Spawn(Vector3 worldPosition)
        {
            return Instantiate(playerPrefab, worldPosition, Quaternion.identity);
        }
        
        private void OnDisconnected(CoherenceBridge _, ConnectionCloseReason __)
        {
            assignedClients = null;
            unusedSpawnPoints = null;
            coherenceBridge.ClientConnections.OnCreated -= AssignSpawnPointToClient;
        }
    }
}
