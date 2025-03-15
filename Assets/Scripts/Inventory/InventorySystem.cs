using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    private ItemState _assaultRifleSlot;
    private ItemState _meleeWeaponSlot;

    public ItemState AssaulsRifleSlot { get { return _assaultRifleSlot; } }
    public ItemState MeleeWeaponSlot { get { return _meleeWeaponSlot; } }

    private InventorySystem()
    {
        _assaultRifleSlot = new ItemState();
        _meleeWeaponSlot = new ItemState();
    }

    public void AddWeapon(BaseWeapon weapon)
    {
        if (weapon is AssaultRifle)
        {
            _assaultRifleSlot.AddItem(weapon);
            Debug.Log("������� �������� � ���������"); //�� �������� ����� ������� ������ ���������� ������ � ������ ������ (�������� ��� ����� PickUp � ������)
        }
        else if (weapon is MeleeWeapon)
        {
            _meleeWeaponSlot.AddItem(weapon);
            Debug.Log("��� �������� � ���������");
        }
    }
}
