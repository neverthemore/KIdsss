using Coherence.Connection;
using Coherence.Toolkit;
using UnityEngine;
using Random = UnityEngine.Random;
using Cinemachine;

public class PlayerHandler : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints; // ������ ����� ������
    public float spawnRadius = 1f; // Fallback ������, ���� ��� �����

    public GameObject prefabToSpawn;

    [Header("Camera")]
    public CinemachineVirtualCamera gameplayVCam;
    public bool lookAtPlayer = true;
    public bool followPlayer = true;

    [Tooltip("��� �������-����� ������ ������� ������")]
    public string anchorName = "CameraAnchor"; // ��� ������� � �������

    private GameObject _player;
    private Transform _cameraAnchor; // ������ �� ����� �� �������
    private CoherenceBridge _bridge;

    [System.Obsolete]
    private void Awake()
    {
        if (gameplayVCam != null) gameplayVCam.gameObject.SetActive(false);
        _bridge = FindObjectOfType<CoherenceBridge>();
        _bridge.onConnected.AddListener(OnConnection);
        _bridge.onDisconnected.AddListener(OnDisconnection);
    }

    private void OnConnection(CoherenceBridge bridge) => SpawnPlayer();
    private void OnDisconnection(CoherenceBridge bridge, ConnectionCloseReason reason) => DespawnPlayer();


    private Vector3 GetRandomSpawnPosition()
    {
        // ���� ���� ����� ������ - �������� ���������
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        }

        // Fallback �� ������ ������ � ��������
        Vector3 initialPosition = transform.position + Random.insideUnitSphere * spawnRadius;
        initialPosition.y = transform.position.y;
        return initialPosition;
    }
    private void SpawnPlayer()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();

        _player = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        _player.name = "[local] Player";

        _cameraAnchor = _player.transform.Find("CameraAim/CameraPivot");

        if (_cameraAnchor == null)
        {
            Debug.LogError($"������ � ������ '{anchorName}' �� ������ � ������� ������!");
            return;
        }

        if (gameplayVCam != null)
        {
            gameplayVCam.transform.position = _cameraAnchor.position;
            gameplayVCam.transform.rotation = _cameraAnchor.rotation;

            gameplayVCam.Follow = _cameraAnchor;
            gameplayVCam.LookAt = _cameraAnchor;
            gameplayVCam.gameObject.SetActive(true);

            CinemachineBrain.SoloCamera = gameplayVCam;
        }
    }

    public void RespawnPlayer()
    {
        if (_player != null)
        {
            Destroy(_player);
        }
        SpawnPlayer();
    }
    private void DespawnPlayer()
    {
        Destroy(_player);
        if (gameplayVCam != null) gameplayVCam.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _bridge.onConnected.RemoveListener(OnConnection);
        _bridge.onDisconnected.RemoveListener(OnDisconnection);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}

