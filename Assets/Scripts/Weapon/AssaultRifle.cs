using UnityEngine;

public class AssaultRifle : BaseWeapon
{
    protected int _currentAmmo;
    protected int _maxAmmo;

    protected override void Attack()
    {
        //���������� �����
        Debug.Log("������� �� ��������");
    }
}
