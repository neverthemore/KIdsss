using UnityEngine;

public class change : MonoBehaviour
{
    [SerializeField] Transform point;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Transform cinemachineVC = GameObject.Find("Cinemachine VC").transform;
        point = cinemachineVC.Find("CameraAimingPoint");
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = point.position;        
    }
}
