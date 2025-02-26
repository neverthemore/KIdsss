using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //Главный скрипт персонажа, содержит его инфу и компоненты
    private InventorySystem _inventorySystem;

    private void Start()
    {
        _inventorySystem = GetComponent<InventorySystem>();
    }

    public void AddWeapon(BaseWeapon weapon)
    {
        _inventorySystem.AddWeapon(weapon);
    }

}
