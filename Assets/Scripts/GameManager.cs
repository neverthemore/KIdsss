using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab; // ������ �������� ������
    public GameObject cameraPrefab; // ������ ������
    public int numberOfPlayers = 4; // ���������� �������

    void Start()
    {
        CreatePlayers();
    }

    void CreatePlayers()
    {
        for (int i = 0; i < numberOfPlayers; i++)
        {
            // �������� ������
            GameObject player = Instantiate(playerPrefab, new Vector3(i * 2.0f, 0, 0), Quaternion.identity);
            player.name = "Player" + (i + 1);

            // �������� ������
            GameObject camera = Instantiate(cameraPrefab);
            camera.name = "Camera" + (i + 1);

            // �������� ������ � ������
            CameraFollow cameraFollow = camera.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.player = player.transform; // ��������� ������ ��� ��������
                cameraFollow.offset = new Vector3(0, 5, -10); // ��������� �������� ������
            }
        }
    }
}