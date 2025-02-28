using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(InventorySystem))]
public class WeaponManager : MonoBehaviour
{
   //����������� ������ � ��������� + ����
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
        if (Input.GetKeyDown(KeyCode.Alpha1))  //��� ������� ����� Controls, ������ ������� ����� ������
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

        _currentState = newWeapon; //�������� �������� �� ��, �� �������� �� ������� �������

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
