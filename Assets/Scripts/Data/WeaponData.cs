using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public GameObject _weaponPrefab;

    public string _name; //В дальнейшем я бы это сделал через ScriptableObj
    public float _damage;
    public float _critDamage; //Например в голову
    public float _durability; //Износ
    public float _attackSpeed;

    public Sprite _icon;
}
