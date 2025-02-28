using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public GameObject _weaponPrefab;

    public string _name; //� ���������� � �� ��� ������ ����� ScriptableObj
    public float _damage;
    public float _critDamage; //�������� � ������
    public float _durability; //�����
    public float _attackSpeed;

    public Sprite _icon;
}
