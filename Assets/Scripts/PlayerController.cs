using Coherence;
using Coherence.Toolkit;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private float _lookSensitivity = 2f;
    private CoherenceSync _sync;
    private Controls controls;

    

    private void Awake()
    {
        _sync = GetComponent<CoherenceSync>();
        controls = GetComponent<Controls>();      
        
    }

    private void Move()
    {
        Vector2 tempMove = controls.GetMoving();
        Debug.Log(controls.GetMoving());
        Vector3 movement = transform.forward * tempMove.y + transform.right * tempMove.x;
        //movement.y = 0f;
        transform.position += (movement * speed * Time.deltaTime).normalized;
        
    }

    private void Update()
    {
        // Движение
        Move();
        /*
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        transform.position += move * speed * Time.deltaTime;
        */
      
    }
}
