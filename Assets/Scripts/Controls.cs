using System.Data;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Controls : MonoBehaviour
{
    public InputSystem inputActions;       
    private void OnEnable() { inputActions.Enable(); }
    private void OnDisable() { inputActions.Disable(); }

    public Vector2 GetMoving() { return inputActions.Player.Moving.ReadValue<Vector2>(); }
    public Vector2 GetLook() { return inputActions.Player.Look.ReadValue<Vector2>(); }
    public bool GetJump() {
        //bool isJumped = inputActions.Player.Jump.triggered;
        bool isJumped = inputActions.Player.Jump.IsPressed();
        return isJumped;        
    }
    public bool GetRun()
    {
        bool isRunning = inputActions.Player.Run.IsPressed();
        return isRunning;       
    }
    public bool GetSit()
    {
        bool isSitted = inputActions.Player.Sit.IsPressed();
        return isSitted;
    }

    public bool GetInteraction()
    {
        bool isInteract = inputActions.Player.Interact.IsPressed();
        return isInteract;
    }

    public bool GetFire()
    {
        bool isFire = inputActions.Player.Fire.IsPressed();
        return isFire;
    }

    void Awake()
    {
        inputActions = new InputSystem();
        inputActions.Enable();
    }
    void Update()
    {
        
    }
}
