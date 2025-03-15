using UnityEngine;

[RequireComponent(typeof(BaseWeapon))]
public class WeaponSelection : MonoBehaviour, IInteractable
{
    //Скрипт на оружии (на сцене) для подбора
    //Возможно для подбираемых предметов нужно сделать базовый класс или интерфейс
    BaseWeapon weapon;
 
    private void Start()
    {
        weapon = GetComponent<BaseWeapon>(); //Получаем оружие со сцены
    }

    public void Interact(PlayerController playerController) //Возможно нужно передать скрипт инвентаря
    {
        //Подбор оружия
        playerController.AddWeapon(weapon);
        weapon.Deactivate();
        //Destroy(gameObject);
    }

    public void ShowPromt()
    {
        //Подсветка о взаимодействии
    }
    public void HidePromt()
    {

    }
}
