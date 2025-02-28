using UnityEngine;

public class AssaultRifle : BaseWeapon
{
    protected int _currentAmmo;
    protected int _maxAmmo;

    public override void Attack()
    {
        //Реализация атаки
        Debug.Log("Выстрел из автомата");
        //Спавним префаб пули
    }
}
