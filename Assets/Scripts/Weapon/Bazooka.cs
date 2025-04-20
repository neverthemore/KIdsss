using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
public class Bazooka : MainWeapon
{
    protected int _currentAmmo;
    [SerializeField] protected int _maxAmmo;
    [SerializeField] private float _range;

    private float _currentCooldown = 0;

    private void Start()
    {
        _currentAmmo = _maxAmmo;
    }

    public override void Attack()
    {
        Debug.Log("Выстрел из базуки");
    }
    
}
