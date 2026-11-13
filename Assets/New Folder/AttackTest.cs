using UnityEditor;
using UnityEngine;

public class AttackTest : MonoBehaviour
{
    public float radius = 3f;

    public Transform target;
    public float attackAngle = 90f;
    private void Update()
    {
        Collider[] list = Physics.OverlapSphere(transform.position, radius);


        foreach (Collider c in list)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0;

            float angle = Vector3.Angle(transform.forward, dir);

            if (angle <= attackAngle / 2)
            {
                Debug.Log("시전자 정면기준 피격자의 각도 : " + angle);
                Debug.Log("히트");
            }
        }
    }
}
