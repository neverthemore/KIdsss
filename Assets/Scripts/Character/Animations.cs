using UnityEngine;

public class Animations : MonoBehaviour
{
    Animator animator;
    MovementComponent movement;
    void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<MovementComponent>();
    }

    void Update()
    {
        /*
        float inputLeft = Input.GetAxis("Horizontal");
        float inputFwd = Input.GetAxis("Vertical");

        bool isSit = Input.GetKey(KeyCode.LeftControl);
        float sit = isSit ? 1f : 0f;

        float isJump = Input.GetAxis("Jump");
        */

        float inputLeft = movement.CharacterController.velocity.x;
        float inputFwd = movement.CharacterController.velocity.y;

        bool isSit = Input.GetKey(KeyCode.LeftControl);
        float sit = isSit ? 1f : 0f;

        float isJump = Input.GetAxis("Jump");



        if (inputLeft != 0f || inputFwd != 0f)
        {
            animator.SetFloat("x", inputLeft);
            animator.SetFloat("y", inputFwd);
            animator.SetFloat("sit", sit);
            animator.SetFloat("jump", isJump);
        }
    }
}
