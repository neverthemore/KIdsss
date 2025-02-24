using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    //Базовый абстрактный класс для оружия
    [Header("Set in inspector")]
    [SerializeField] protected string _name; //В дальнейшем я бы это сделал через ScriptableObj
    [SerializeField] protected float _damage;
    [SerializeField] protected float _critDamage; //Например в голову
    [SerializeField] protected float _durability; //Износ
    [SerializeField] protected float _attackSpeed;

    protected abstract void Attack();

    protected virtual void PickUp()
    {
        Debug.Log($"{_name} подобран");
    }


    protected virtual void Drop()
    {
        Debug.Log($"{_name} выброшен");
    }

    protected virtual void Upgrade()
    {
        //Если будет какое-то улучшение/починка
    }

    protected bool IsFunctional()
    {
        return _durability > 0;
    }
}
