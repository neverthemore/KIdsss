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

    [SerializeField] Transform WeaponPose;     

    float inputLeft;    
    float inputFwd;         

    bool isSit;
    float sit;    

    bool isJump;    
    float jump;
    
    bool isRun;
    float run;

    [SerializeField] bool withMainGun;
    [SerializeField] bool withSecondGun;
    void Start()
    {
        _coherenceSync = GetComponent<CoherenceSync>();
        animator = GetComponent<Animator>();        
        controls = GetComponent<Controls>();
        inventory = GetComponent<WeaponManager>();
        inputLeft = 0f;
        inputFwd = 0f;
    }

    void ToAnimator()
    {
        if (!_coherenceSync.HasStateAuthority) return;

        animator.SetFloat("x", inputLeft);
        animator.SetFloat("y", inputFwd);
        animator.SetFloat("sit", sit);
        animator.SetFloat("jump", jump);
        animator.SetFloat("run", run);        
        animator.SetBool("mainGun", withMainGun);
        animator.SetBool("secondGun", withSecondGun);
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
        // прыжок не работает:((
        float raycastDistance = 0.1f;
        int ground;
        ground = LayerMask.NameToLayer("ground");
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, ground))
            isJump = false;
        else isJump = true;            
        jump = isJump ? 1f : 0f;        

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

        ToAnimator();
    }
}
