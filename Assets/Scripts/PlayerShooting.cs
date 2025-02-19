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
        // ��������� ��� ��������� ���� ��������
      

        if (Input.GetButtonDown("Fire1"))
        {
            // ���������� ������� ����� Coherence
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

        // ��������� ���
        GameObject bullet = Instantiate(_bulletPrefab, position, Quaternion.LookRotation(direction));

        // ����������� ������������� ����
        var bulletSync = bullet.GetComponent<CoherenceSync>();
        bulletSync.RequestAuthority(AuthorityType.Full);

        // ������ ��������
        bullet.GetComponent<Rigidbody>().velocity = direction * 50f;
    }

    
}