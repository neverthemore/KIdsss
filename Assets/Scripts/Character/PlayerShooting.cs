using Coherence;
using Coherence.Toolkit;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;
    private CoherenceSync _sync;

    private void Awake()
    {
        _sync = GetComponent<CoherenceSync>();
    }

    private void Update()
    {
        // Проверяем что управляем этим объектом
      

        if (Input.GetButtonDown("Fire1"))
        {
            // Отправляем команду через Coherence
            _sync.SendCommand<PlayerShooting>(
                nameof(ShootCommand),
                MessageTarget.Other,
                _firePoint.position,
                _firePoint.forward
            );

            
        }
    }

    [Command]
    [System.Obsolete]
    public void ShootCommand(Vector3 position, Vector3 direction)
    {

        // Серверный код
        GameObject bullet = Instantiate(_bulletPrefab, position, Quaternion.LookRotation(direction));

        // Настраиваем синхронизацию пули
        var bulletSync = bullet.GetComponent<CoherenceSync>();
        bulletSync.RequestAuthority(AuthorityType.Full);

        // Задаем скорость
        bullet.GetComponent<Rigidbody>().velocity = direction * 50f;
    }

    
}