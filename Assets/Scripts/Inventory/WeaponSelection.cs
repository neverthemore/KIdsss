using UnityEngine;

[RequireComponent(typeof(BaseWeapon))]
public class WeaponSelection : MonoBehaviour, IInteractable
{
    //������ �� ������ (�� �����) ��� �������
    //�������� ��� ����������� ��������� ����� ������� ������� ����� ��� ���������
    BaseWeapon weapon;

    

    private void Start()
    {
        weapon = GetComponent<BaseWeapon>(); //�������� ������ �� �����
    }

    public void Interact(PlayerController playerController) //�������� ����� �������� ������ ���������
    {
        //������ ������
        playerController.AddWeapon(weapon);
        Destroy(gameObject);
    }

    public void ShowPromt()
    {
        //��������� � ��������������
    }
    public void HidePromt()
    {

    }
}
