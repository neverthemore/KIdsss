using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    private ItemState AssaultRifleSlot;
    private ItemState PistolSlot;
    private ItemState MeleeWeaponSlot;

    private ItemState _currentWeapon;

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
            Debug.Log("Автомат добавлен в инвентарь"); //По хорошему лучше сделать логику добавления оружия у самого оружия (например как метод PickUp у оружия)
        }
        else if (weapon is Pistol)
        {
            PistolSlot.AddItem(weapon);
            Debug.Log("Пистолет добавлен в инвентарь");
        }
        else if (weapon is MeleeWeapon)
        {
            MeleeWeaponSlot.AddItem(weapon);
            Debug.Log("Нож добавлен в инвентарь");
        }
    }
}
