using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

public class AssaultRifle : BaseWeapon
{
    protected int _currentAmmo;
    [SerializeField] protected int _maxAmmo;
    [SerializeField] private float _range;
    //[SerializeField] private Camera _playerCamera;    

    private float _currentCooldown = 0;

    private void Start()
    {
        _currentAmmo = _maxAmmo;        
    }

    public override void Attack()
    {
        //Реализация атаки
        if (_currentCooldown > 0)
        {
            _currentCooldown -= Time.deltaTime;
            return;
        }
        RaycastHit hit;

        if (Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward, out hit, _range))
        {
            ProcessHit(hit);
            _currentCooldown = _weaponData._attackSpeed;
        }
        _currentCooldown = _weaponData._attackSpeed;

        Debug.DrawRay(_playerCamera.transform.position,
             _playerCamera.transform.forward * _range,
             Color.red,
             1f);

    }

    void ProcessHit(RaycastHit hit)
    {
        IDamageable target = hit.collider.GetComponent<IDamageable>();
        target?.TakeDamage(_weaponData._damage);
    }
}
