using UnityEngine;

public interface IInteractable
{    
    public void Interact(PlayerController playerController);

    public void ShowPromt();

    public void HidePromt();
}
