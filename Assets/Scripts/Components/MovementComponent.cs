using Coherence.Toolkit;
using UnityEngine;

[RequireComponent (typeof(CharacterController))]
public class MovementComponent : MonoBehaviour, IInitializable
{
    [Header("Speed")]
    [SerializeField] private float _walkSpeed = 0.1f;
    [SerializeField] private float _runSpeed = 0.15f;

    [Header("Sens")][SerializeField] 
    private float _lookSensetivity = 1f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 0.2f;
    [SerializeField] private float _gravityForce = -9.81f / 5;  //Делю на 5 из-за скейла модельки
    float _jumpUp;

    private CharacterController _characterController;
    private CoherenceSync _sync;
    private Controls controls;

    private float _currentSpeed;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _sync = GetComponent<CoherenceSync>();
        controls = GetComponent<Controls>();
        _characterController = GetComponent<CharacterController>();
    }
    private void FixedUpdate()
    {
        Run();
        Move();
        Jump();
        Sit();
        
    }

    private void Move()
    {
        if (!_characterController.isGrounded)
        {
            _jumpUp += _gravityForce * Time.deltaTime;
        }
        Vector2 direction = controls.GetMoving().normalized;
        Vector3 move = new Vector3(direction.x * _currentSpeed, _jumpUp, direction.y * _currentSpeed);
        _characterController.Move(move);
    }

    private void Jump()
    {
        if (_characterController.isGrounded && controls.GetJump())
        {
            _jumpUp = _jumpForce;
        }
    }

    private void Sit()
    {
        if (controls.GetSit() != 0) Debug.Log(controls.GetSit());
    }

    private void Run()
    {
        if (controls.GetRun()) _currentSpeed = _runSpeed;
        else _currentSpeed = _walkSpeed;
    }
}
