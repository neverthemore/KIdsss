using Coherence;
using Coherence.Toolkit;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _lookSensitivity = 2f;
    private CoherenceSync _sync;
    private Transform _cameraTransform;

    private void Awake()
    {
        _sync = GetComponent<CoherenceSync>();
        _cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    private void Update()
    {
      

        // Движение
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        transform.position += move * _speed * Time.deltaTime;

        // Вращение
        float mouseX = Input.GetAxis("Mouse X") * _lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * _lookSensitivity;
        transform.Rotate(0, mouseX, 0);
        _cameraTransform.Rotate(-mouseY, 0, 0);
    }
}
