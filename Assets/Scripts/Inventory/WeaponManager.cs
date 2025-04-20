//using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(InventorySystem))]
public class WeaponManager : MonoBehaviour
{
   //Переключает оружия в инвентаре + ввод
    private InventorySystem _inventorySystem;
    private ItemState _currentState;

    private Controls _controls;  
    public ItemState CurrentState { get { return _currentState; } }

    [SerializeField] private Transform _weaponParent;
    Animations _animations;    
    private void Start()
    {
        _inventorySystem = GetComponent<InventorySystem>();
        _controls = GetComponent<Controls>();        
        _animations = GetComponent<Animations>();
    }
    private void Update()
    {
        HandleWeaponSwitchInput();
        if (_controls.GetBreakingOfGun())
        {
            BreakMainWeapon();
        }
            
        if (_controls.GetFire())
        {
            Attack();
        }
    }   

    private void HandleWeaponSwitchInput()
    {
        if (_controls.GetMainWeapon())
        {
            if (_inventorySystem.AssaulsRifleSlot.Item != null)
                SwitchWeapon(_inventorySystem.AssaulsRifleSlot);
        }
        if (_controls.GetSecondWeapon())
        {
            if (_inventorySystem.MeleeWeaponSlot.Item != null)
                SwitchWeapon(_inventorySystem.MeleeWeaponSlot);
            else Debug.Log("во втором слоте ничего");
        }
    }

    private void BreakMainWeapon()
    {
        if (_inventorySystem.AssaulsRifleSlot.Item != null &&
            _currentState.Item is MainWeapon)
        {
            if (_currentState.Item is AssaultRifle)
            {           
                //добавить милишку
                GameObject swordPrefab = Resources.Load<GameObject>("Prefabs/Weapons/Sword");
                if (swordPrefab != null)
                {
                    GameObject spawnedSword = Instantiate(swordPrefab, transform.position, Quaternion.identity);
                    spawnedSword.transform.SetParent(_weaponParent);
                    Sword tempMeleeWeapon = spawnedSword.GetComponent<Sword>();
                    _inventorySystem.AddWeapon(tempMeleeWeapon);
                }
                //поменять слот
                SwitchWeapon(_inventorySystem.MeleeWeaponSlot);
            }
            Debug.Log("Замена основного оружия");
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
            Transform weapon = transform.Find("WeaponPoint").GetChild(0).GetChild(0);
            Transform leftPlace = weapon.Find("LeftArmSpace");
            Transform rightPlace = weapon.Find("RightArmSpace");
            _animations.HandsToGun(leftPlace, rightPlace);
        }
        else
        {
            _animations.ReleaseHands();
        }
    }   

    private void Attack()
    {
        if (_currentState == null || _currentState.Item == null) return;
        _currentState.Item.Attack();
    }

}
