using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(InventorySystem))]
public class WeaponManager : MonoBehaviour
{
   //Переключает оружия в инвентаре + ввод
   private InventorySystem _inventorySystem;
   private ItemState _currentState;
   public ItemState CurrentState { get { return _currentState; } }

    [SerializeField] private Transform _weaponParent;

    private void Start()
    {
        _inventorySystem = GetComponent<InventorySystem>();
    }
    private void Update()
    {
        HandleWeaponSwitchInput();

        if (Input.GetMouseButtonDown(0))        
            Attack();
        
            

    }

    private void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))  //Тут сделать через Controls, сейчас сделано через кейпад
        {
            if (_inventorySystem.AssaulsRifleSlot.Item != null)
            SwitchWeapon(_inventorySystem.AssaulsRifleSlot);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (_inventorySystem.MeleeWeaponSlot.Item != null)
                SwitchWeapon(_inventorySystem.MeleeWeaponSlot);
        }
    }

    private void SwitchWeapon(ItemState newWeapon)
    {
        if (_currentState != null)
        if (_currentState.Item != null)
        {
            _currentState.Deactivate();
        }

        _currentState = newWeapon; //Добавить проверку на то, не является ли текущим оружием

        if (_currentState.Item != null)
        {
            _currentState?.Activate(_weaponParent);
        }
    }

    private void Attack()
    {
        if (_currentState == null || _currentState.Item == null) return;
        _currentState.Item.Attack();
    }

}
