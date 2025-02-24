using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    //������� ����������� ����� ��� ������
    [Header("Set in inspector")]
    [SerializeField] protected string _name; //� ���������� � �� ��� ������ ����� ScriptableObj
    [SerializeField] protected float _damage;
    [SerializeField] protected float _critDamage; //�������� � ������
    [SerializeField] protected float _durability; //�����
    [SerializeField] protected float _attackSpeed;

    protected abstract void Attack();

    protected virtual void PickUp()
    {
        Debug.Log($"{_name} ��������");
    }


    protected virtual void Drop()
    {
        Debug.Log($"{_name} ��������");
    }

    protected virtual void Upgrade()
    {
        //���� ����� �����-�� ���������/�������
    }

    protected bool IsFunctional()
    {
        return _durability > 0;
    }
}
