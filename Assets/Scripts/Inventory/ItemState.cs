using UnityEngine;

public class ItemState
{
    public BaseWeapon Item { get; private set; }   
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

    public void Activate(Transform parent)
    {
        Item.Activate(parent);
    }

    public void Deactivate()
    {
        Item.Deactivate();
    }

}
