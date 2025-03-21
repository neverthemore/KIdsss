using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    //Базовый абстрактный класс для оружия
    //[Header("Set in inspector")]
    [SerializeField] public GameObject _playerCamera;
    public WeaponData _weaponData;
   
    public abstract void Attack();

    public void Activate(Transform parent)
    {        
        transform.SetParent( parent );
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.SetActive( true );
    }

    public void Deactivate()
    {
        gameObject.SetActive( false );
        transform.SetParent(null);
    }
}
