using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(InventorySystem))]
public class WeaponManager : MonoBehaviour
{
   //����������� ������ � ��������� + ����
    private InventorySystem _inventorySystem;
    private ItemState _currentState;

    private Controls _controls;  
    public ItemState CurrentState { get { return _currentState; } }

    private Animations _animations;

    [SerializeField] private Transform _weaponParent;

    private void Start()
    {
        _inventorySystem = GetComponent<InventorySystem>();
        _controls = GetComponent<Controls>();
        _animations = GetComponent<Animations>();
        
    }
    private void Update()
    {
        HandleWeaponSwitchInput();
        
        if (_controls.GetFire())
        {
            Attack();
        }
    }   

    private void HandleWeaponSwitchInput()
    {
        if (_controls.GetMainWeapon())  //��� ������� ����� Controls, ������ ������� ����� ������
        {//������ ����� controls
            if (_inventorySystem.AssaulsRifleSlot.Item != null)
            SwitchWeapon(_inventorySystem.AssaulsRifleSlot);
        }
        if (_controls.GetSecondWeapon())
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
            Transform left = GameObject.Find("LeftArmSpace").transform;
            Transform right = GameObject.Find("RightArmSpace").transform;
            _animations.HandsToGun(left, right);
            //��� ��� � ����� ������ ������������ ��� ��� ���� (IK Constraint)
        }
    }

    private void Attack()
    {
        if (_currentState == null || _currentState.Item == null) return;
        _currentState.Item.Attack();
    }

}
