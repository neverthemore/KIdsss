using UnityEngine;

public class Animations : MonoBehaviour
{
    Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float inputLeft = Input.GetAxis("Horizontal");
        float inputFwd = Input.GetAxis("Vertical");
        if (inputLeft != 0f || inputFwd != 0f)
        {
            animator.SetFloat("x", inputLeft);
            animator.SetFloat("y", inputFwd);
        }
    }
}
