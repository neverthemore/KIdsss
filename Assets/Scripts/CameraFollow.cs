using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; // Ссылка на трансформ игрока
    public Vector3 offset; // Смещение камеры относительно игрока
    public float smoothSpeed = 0.125f; // Скорость сглаживания движения камеры

    void LateUpdate()
    {
        if (player != null)
        {
            Vector3 desiredPosition = player.position + offset; // Желаемая позиция камеры
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed); // Сглаженная позиция
            transform.position = smoothedPosition; // Установка позиции камеры

            transform.LookAt(player); // Поворот камеры к игроку
        }
    }
}