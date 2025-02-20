using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab; // Префаб главного игрока
    public GameObject cameraPrefab; // Префаб камеры
    public int numberOfPlayers = 4; // Количество игроков

    void Start()
    {
        CreatePlayers();
    }

    void CreatePlayers()
    {
        for (int i = 0; i < numberOfPlayers; i++)
        {
            // Создание игрока
            GameObject player = Instantiate(playerPrefab, new Vector3(i * 2.0f, 0, 0), Quaternion.identity);
            player.name = "Player" + (i + 1);

            // Создание камеры
            GameObject camera = Instantiate(cameraPrefab);
            camera.name = "Camera" + (i + 1);

            // Привязка камеры к игроку
            CameraFollow cameraFollow = camera.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.player = player.transform; // Установка игрока для слежения
                cameraFollow.offset = new Vector3(0, 5, -10); // Установка смещения камеры
            }
        }
    }
}