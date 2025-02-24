using UnityEngine;

public class MeleeWeapon : BaseWeapon
{
    protected override void Attack()
    {
        //Реализация атаки
        Debug.Log("Удар ножом");
    }
}
