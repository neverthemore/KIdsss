using UnityEngine;

public class Pistol : BaseWeapon
{
    protected int _currentAmmo;
    protected int _maxAmmo;


    public override void Attack()
    {
        //Реализация атаки
        Debug.Log("Выстрел из пистолета");
    }
}
