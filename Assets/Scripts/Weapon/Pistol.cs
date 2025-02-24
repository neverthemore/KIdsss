using UnityEngine;

public class Pistol : BaseWeapon
{
    protected int _currentAmmo;
    protected int _maxAmmo;

    private Pistol()
    {
        _name = "��������";
        _damage = 10;
        _attackSpeed = 1.5f;
        _maxAmmo = 12;
        _durability = 100;
    }

    protected override void Attack()
    {
        //���������� �����
        Debug.Log("������� �� ���������");
    }
}
