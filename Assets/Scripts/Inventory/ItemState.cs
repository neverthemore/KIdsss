using UnityEngine;

public class ItemState
{
    private BaseWeapon Item;
    //private bool IsEmpty => Item == null;

    public void AddItem(BaseWeapon item)
    {
        Item = item;
    }

    public void RemoveItem()
    {
        Item = null;
        //+ ������� ��������� ����� ������ � �������� �� �����
    }


}
