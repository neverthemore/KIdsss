using UnityEngine;
using UnityEngine.Rendering.UI;

public class InventorySystem : MonoBehaviour
{
    private ItemState _assaultRifleSlot;
    private ItemState _meleeWeaponSlot;
    private ItemState _itemSlot;

    public ItemState AssaulsRifleSlot { get { return _assaultRifleSlot; } }
    public ItemState MeleeWeaponSlot { get { return _meleeWeaponSlot; } }
    public ItemState ItemSlot { get { return _meleeWeaponSlot; } }

    private void Start()
    {
        _assaultRifleSlot = new ItemState();
        _meleeWeaponSlot = new ItemState();
        _itemSlot = new ItemState();
    }  

    public void AddWeapon(BaseWeapon weapon)
    {
        if (weapon is AssaultRifle)
        {
            _assaultRifleSlot.AddItem(weapon);
            Debug.Log("Автомат добавлен в инвентарь"); //По хорошему лучше сделать логику добавления оружия у самого оружия (например как метод PickUp у оружия)
        }
        else if (weapon is Pistol)
        {
            _assaultRifleSlot.AddItem(weapon);
            Debug.Log("Пистолет добавлен в инвентарь");
        }
        else if (weapon is Bazooka)
        {
            _assaultRifleSlot.AddItem(weapon);
            Debug.Log("Базука добавлена в инвентарь");
        }
        else if (weapon is MeleeWeapon)
        {
            _meleeWeaponSlot.AddItem(weapon);
            Debug.Log("Нож добавлен в инвентарь");
        }
    }
    
}
