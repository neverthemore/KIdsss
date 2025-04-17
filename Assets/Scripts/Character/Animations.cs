using UnityEngine;
using UnityEngine.Animations.Rigging;
using Coherence.Toolkit; // Добавьте в начало файла
using Coherence;
public class Animations : MonoBehaviour
{
    private Animator animator;
    private MovementComponent movement;
    private Controls controls;
    private WeaponManager inventory;
    private CoherenceSync _coherenceSync;
    private RigBuilder _rigBuilder;

    [SerializeField] Transform WeaponPose;     

    float inputLeft;    
    float inputFwd;         

    bool isSit;
    float sit;    

    [SerializeField] bool isJump;    
    float jump;
    
    bool isRun;
    float run;

    [Header("Footstep Sounds")]
    [SerializeField] private AudioSource _footstepAudio;
    

    [SerializeField] bool withMainGun;
    [SerializeField] bool withSecondGun;
    void Start()
    {
        movement = GetComponent<MovementComponent>();
        _coherenceSync = GetComponent<CoherenceSync>();
        animator = GetComponent<Animator>();        
        controls = GetComponent<Controls>();
        inventory = GetComponent<WeaponManager>();
        inputLeft = 0f;
        inputFwd = 0f;
        _footstepAudio = GetComponent<AudioSource>();    
        _rigBuilder = GetComponent<RigBuilder>();
    }

    void ToAnimator()
    {
        if (!_coherenceSync.HasStateAuthority) return;

        animator.SetFloat("x", inputLeft);
        animator.SetFloat("y", inputFwd);
        animator.SetFloat("sit", sit);
        animator.SetFloat("jump", movement.GetJumpUp());
        animator.SetFloat("run", run);        
        animator.SetBool("mainGun", withMainGun);
        animator.SetBool("secondGun", withSecondGun);
    }
    public void ReleaseHands()
    {        
        _rigBuilder.layers[3].active = false;
    }
    public void HandsToGun(Transform left, Transform right)
    {
        TwoBoneIKConstraint[] constraints = GetComponentsInChildren<TwoBoneIKConstraint>();
        constraints[0].data.target = right;
        constraints[1].data.target = left;
        _rigBuilder.layers[3].active = true;
        _rigBuilder.Build();
    }
    void Update()
    {       
        WeaponPose.localEulerAngles = new Vector3 (0f, 0f, 0f);
        if (controls.GetMoving() == new Vector2(0f, 0f))
        {
            inputLeft = 0f;
            inputFwd = 0f;
           
        }           
        else 
        {
            inputLeft = controls.GetMoving().x;
            inputFwd = controls.GetMoving().y;
        }  

        isSit = controls.GetSit();
        sit = isSit ? 1f : 0f;

        isRun = controls.GetRun();
        run = (isRun && inputFwd != 0f) ? 1f : 0f;        

        if (inventory.CurrentState == null)
        {
            withMainGun = false;
            withSecondGun = false;           
        }
        else if (inventory.CurrentState.Item is MeleeWeapon)
        {
            withMainGun = false;
            withSecondGun = true;            
        }
        else
        {
            withMainGun = true;
            withSecondGun = false;                    
                  
        }
        if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.35f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.35f)
        {
            if (_footstepAudio.isPlaying) return;
            _footstepAudio.Play();
        }
        else
        {
            _footstepAudio.Stop();
        }
        ToAnimator();
    }  
}
