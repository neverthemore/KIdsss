using UnityEngine;
using Coherence;
using Coherence.Toolkit;
public class PlayerVisibility : MonoBehaviour
{
    [SerializeField] private GameObject MyBody; 
    [SerializeField] private GameObject OthersBody; 

    private CoherenceSync coherenceSync;

    private void Start()
    {
        coherenceSync = GetComponent<CoherenceSync>();

        // ���� ��� ��������� �����, ��������� OthersBody
        if (coherenceSync.HasStateAuthority)
        {
            OthersBody.SetActive(false);
            MyBody.SetActive(true);
        }
        else
        {
            // ��� ������ ������� ��������� MyBody
            MyBody.SetActive(false);
            OthersBody.SetActive(true);
        }
    }
}
