using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    private ItemState AssaultRifleSlot;
    private ItemState PistolSlot;
    private ItemState MeleeWeaponSlot;

    private InventorySystem()
    {
        AssaultRifleSlot = new ItemState();
        PistolSlot = new ItemState();
        MeleeWeaponSlot = new ItemState();
    }

    public void AddWeapon(BaseWeapon weapon)
    {
        if (weapon is AssaultRifle)
        {
            AssaultRifleSlot.AddItem(weapon);
            Debug.Log("������� �������� � ���������");
        }
        else if (weapon is Pistol)
        {
            PistolSlot.AddItem(weapon);
            Debug.Log("�������� �������� � ���������");
        }
        else if (weapon is MeleeWeapon)
        {
            MeleeWeaponSlot.AddItem(weapon);
            Debug.Log("��� �������� � ���������");
        }
    }
}
