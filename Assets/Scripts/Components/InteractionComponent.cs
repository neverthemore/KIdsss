using Cinemachine;
using UnityEngine;

public class InteractionComponent : MonoBehaviour
{
    //Скрипт для взаимодействия персонажа с миром (например подбор оружия)
    [SerializeField] private float _interactRange = 1f;
    [SerializeField] private LayerMask _interactableLayers;

    private IInteractable _currentInteractable;
    private Controls _controls;

    private void Start()
    {
        _controls = GetComponent<Controls>();
    }


    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, _interactRange, _interactableLayers))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                ResetCurrentInteractable();
                _currentInteractable = interactable;
                _currentInteractable.ShowPromt();
            }
            if (_controls.GetInteraction()) interactable.Interact(GetComponent<PlayerController>());
        }
    }

    private void ResetCurrentInteractable()
    {
        if (_currentInteractable != null)
        {
            _currentInteractable.HidePromt();
            _currentInteractable = null;
        }
    }
}
