using Coherence.Connection;
using Coherence.Toolkit;
using UnityEngine;
using Random = UnityEngine.Random;
using Cinemachine;

public class PlayerHandler : MonoBehaviour
{
    public float spawnRadius = 1f;
    public GameObject prefabToSpawn;

    [Header("Camera")]
    public CinemachineVirtualCamera gameplayVCam;
    public bool lookAtPlayer = true;
    public bool followPlayer = true;

    [Tooltip("Имя объекта-якоря внутри префаба игрока")]
    public string anchorName = "CameraAnchor"; // Имя объекта в префабе

    private GameObject _player;
    private Transform _cameraAnchor; // Ссылка на точку из префаба
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

    private void SpawnPlayer()
    {
        Vector3 initialPosition = transform.position + Random.insideUnitSphere * spawnRadius;
        initialPosition.y = transform.position.y;

        _player = Instantiate(prefabToSpawn, initialPosition, Quaternion.identity);
        _player.name = "[local] Player";

        // Находим точку внутри префаба
        _cameraAnchor = _player.transform.Find("CameraAim/CameraPivot");

        if (_cameraAnchor == null)
        {
            Debug.LogError($"Объект с именем '{anchorName}' не найден в префабе игрока!");
            return;
        }

        if (gameplayVCam != null)
        {
            gameplayVCam.transform.position = _cameraAnchor.position;
            gameplayVCam.transform.rotation = _cameraAnchor.rotation;

            gameplayVCam.Follow = _cameraAnchor;
            gameplayVCam.LookAt = _cameraAnchor;
            gameplayVCam.gameObject.SetActive(true);

            // 5. Принудительно обновляем камеру
            CinemachineBrain.SoloCamera = gameplayVCam;
        }

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

