using UnityEngine;

public class Animations : MonoBehaviour
{
    Animator animator;
    MovementComponent movement;
    Controls controls;

    [SerializeField]
    float inputLeft;
    [SerializeField]
    float inputFwd;

    [SerializeField]
    bool isSit;
    float sit;

    [SerializeField]
    bool isJump;
    float jump;

    [SerializeField]
    bool isRun;
    float run;
    void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<MovementComponent>();
        controls = GetComponent<Controls>();
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
    }


    void FixedUpdate()
    {
        if (controls.GetMoving() == new Vector2(0f, 0f))
        {
            inputLeft = 0f;
            inputFwd = 0f;
        }           
        else 
        {
            inputLeft = transform.InverseTransformDirection(movement.CharacterController.velocity).x;
            inputFwd = transform.InverseTransformDirection(movement.CharacterController.velocity).z;
        }
        isJump = !movement.CharacterController.isGrounded;
        jump = isJump ? 1f : 0f;

        isSit = controls.GetSit();
        sit = isSit ? 1f : 0f;

        isRun = controls.GetRun();
        run = isRun ? 1f : 0f;

        ToAnimator();
    }
}
