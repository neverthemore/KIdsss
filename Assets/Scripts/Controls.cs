using System.Data;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
public class Controls : MonoBehaviour
{
    public InputSystem inputActions;       
    private void OnEnable() { inputActions.Enable(); }
    private void OnDisable() { inputActions.Disable(); }

    public Vector2 GetMoving() { return inputActions.Player.Moving.ReadValue<Vector2>(); }
    public Vector2 GetLook() { return inputActions.Player.Look.ReadValue<Vector2>(); }
    public float GetJump() { 
        bool isJumped = inputActions.Player.Jump.triggered;
        return isJumped ? 1f : 0f;
    }
    public float GetRun()
    {
        bool isRunning = inputActions.Player.Run.ReadValue<bool>();
        return isRunning ? 1f : 0f;
    }
    public float GetSit()
    {
        bool isSitted = inputActions.Player.Sit.ReadValue<bool>();
        return isSitted ? 1f : 0f;
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
