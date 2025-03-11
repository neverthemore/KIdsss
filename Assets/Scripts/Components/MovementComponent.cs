using Cinemachine;
using Coherence.Toolkit;
using UnityEngine;
//using UnityEngine.UIElements;

[RequireComponent (typeof(CharacterController))]
public class MovementComponent : MonoBehaviour, IInitializable
{
    

    [Header("Speed")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _runSpeed = 3f;
    [SerializeField] private float _sitSpeed = 1f;

    [Header("Sens")][SerializeField] 
    private float _lookSensetivity = 100f;
    [SerializeField] public Transform _cameraPivot;
    [SerializeField] public Transform _cameraAim;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 3.4f;
    [SerializeField] private float _gravityForce = -9.81f;  //Делю на 5 из-за скейла модельки
    float _jumpUp;

    //public Transform spine;    
    //[SerializeField] private Transform head;
    //public Transform chara;

    private CharacterController _characterController;
    public CharacterController CharacterController { get { return _characterController; } } //Анимация
    private CoherenceSync _sync;
    private Controls controls;

    private float _currentSpeed;
    private float coreRot;
    private float xRotation;    
    private float yRotation;

    private void Start()
    {        
        Initialize();
    }

    public void Initialize()
    {
        _sync = GetComponent<CoherenceSync>();
        controls = GetComponent<Controls>();
        _characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void Update()
    {
        MouseRotate();  
        Run();        
        Sit();
        Move();
        Jump();
        //OnAnimatorMove();   не убирайте это пж я хз без этого не работает     
    }

    private void Move()
    {
        if (!_characterController.isGrounded)
        {
            _jumpUp += _gravityForce * Time.deltaTime;
        }
        else if (_jumpUp <= 0) _jumpUp = 0;
        
        Vector2 direction = controls.GetMoving().normalized;
        Vector3 move = new Vector3();        
        
        move = transform.forward * direction.y + transform.right * direction.x;
        if (controls.GetRun() && direction.y < 0)
            if (_currentSpeed == _runSpeed)
                _currentSpeed = _walkSpeed;
        
        move.y = _jumpUp;
        move *= _currentSpeed;
        _characterController.Move(move * Time.deltaTime);        
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
        if (controls.GetSit()) _currentSpeed = _sitSpeed;   //Возможно тут лучше StateMachine, тк через if делать не очень + сейчас баг с приседом есть
    }

    private void Run()
    {
        if (controls.GetRun()) _currentSpeed = _runSpeed;
        else _currentSpeed = _walkSpeed;
    }

    private void OnAnimatorMove()
    {
        if (!_characterController.isGrounded)
        {
            _jumpUp += _gravityForce * Time.deltaTime;
        }
        else if (_jumpUp <= 0) _jumpUp = 0;

        Vector2 direction = controls.GetMoving().normalized;
        Vector3 move = new Vector3();
        
        if (controls.GetRun() && direction.y < 0)
            if (_currentSpeed == _runSpeed)
                _currentSpeed = _walkSpeed;

        move = _cameraAim.forward * direction.y + _cameraAim.right * direction.x;
        move.y = _jumpUp;
        move *= _currentSpeed;
        _characterController.Move(move * Time.deltaTime);
        
        float mouseX = controls.GetLook().x * _lookSensetivity * Time.fixedDeltaTime;
        transform.Rotate(Vector3.up * mouseX);
    }

    private void MouseRotate()
    {
        float mouseX = controls.GetLook().x * _lookSensetivity * Time.fixedDeltaTime;
        float mouseY = controls.GetLook().y * _lookSensetivity * Time.fixedDeltaTime;

        xRotation -= mouseY;
        yRotation += mouseX;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        _cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);        
        transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);        
    }
}
