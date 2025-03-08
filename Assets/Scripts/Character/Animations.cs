using UnityEngine;

public class Animations : MonoBehaviour
{
    private Animator animator;
    private MovementComponent movement;
    private Controls controls;
    private WeaponManager inventory;

    [SerializeField]
    Transform rightArmSpace;

    [SerializeField]
    Transform leftArmController;
    Transform leftArmSpace;

    [SerializeField]
    float inputLeft;
    [SerializeField]
    float inputFwd;

    [SerializeField]
    float yRot;

    [SerializeField]
    bool isSit;
    float sit;

    [SerializeField]
    bool isJump;    
    float jump;

    [SerializeField]
    bool isRun;
    float run;

    [SerializeField] bool withMainGun;
    [SerializeField] bool withSecondGun;
    void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<MovementComponent>();
        controls = GetComponent<Controls>();
        inventory = GetComponent<WeaponManager>();
        inputLeft = 0f;
        inputFwd = 0f;
    }

    void ToAnimator()
    {
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
            rightArmSpace.localEulerAngles = new Vector3(63.705f, -49.353f, 38.923f);
            rightArmSpace.localPosition = new Vector3(0.01f, 0.042f, 0.145f);

            if (rightArmSpace.transform.Find("AssaultRifle") != null)
                if (rightArmSpace.transform.Find("AssaultRifle").Find("leftHandPlace") != null)
                {
                    leftArmSpace = rightArmSpace.transform.Find("AssaultRifle").Find("leftHandPlace");
                    leftArmController.position = leftArmSpace.position;
                }
            
            
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
