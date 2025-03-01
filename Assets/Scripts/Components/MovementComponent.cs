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
    [SerializeField] private float _jumpForce = 2f;
    [SerializeField] private float _gravityForce = -9.81f;  //Делю на 5 из-за скейла модельки
    float _jumpUp;

    public Transform spine;    
    public Transform head;
    public Transform chara;

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
        
    }

    private void Move()
    {
        if (!_characterController.isGrounded)
        {
            _jumpUp += _gravityForce * Time.deltaTime;
        }
        else if (_jumpUp < 0) _jumpUp = 0;

      
        Vector2 direction = controls.GetMoving().normalized;
        Vector3 move = new Vector3();

        //Хз че за код (мой код)
            move = _cameraAim.forward * direction.y * _currentSpeed + _cameraAim.right * direction.x * _currentSpeed;
        if (controls.GetRun() && direction.y < 0)
            move = new Vector3(0f, 0f, 0f);
        //        
        move.y = _jumpUp; 
        
        _characterController.Move(move * Time.fixedDeltaTime);        
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

    private void MouseRotate()
    {
        float mouseX = controls.GetLook().x * _lookSensetivity * Time.fixedDeltaTime;
        float mouseY = controls.GetLook().y * _lookSensetivity * Time.fixedDeltaTime;

        xRotation -= mouseY;
        yRotation += mouseX;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        _cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);        
        _cameraAim.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        spine.Rotate(Vector3.up * mouseX);
        head.localRotation = Quaternion.Euler(xRotation, spine.localEulerAngles.y, 0f);

        if (Mathf.Abs(_cameraAim.eulerAngles.y - chara.eulerAngles.y) > 90f)
        {
            chara.localRotation = Quaternion.Lerp(chara.rotation, _cameraAim.rotation, Time.deltaTime*3);
            spine.localRotation = Quaternion.Lerp(spine.localRotation, Quaternion.Euler(0f, 0f, 0f), Time.deltaTime*3);            
        }

        if (controls.GetMoving() != Vector2.zero)
        {
            chara.localRotation = _cameraAim.rotation;
            spine.localRotation = Quaternion.Lerp(spine.localRotation, Quaternion.Euler(0f, 0f, 0f), Time.deltaTime * 3);
        }

    }
}
