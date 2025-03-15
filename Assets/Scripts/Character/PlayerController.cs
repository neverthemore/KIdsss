using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //������� ������ ���������, �������� ��� ���� � ����������
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
