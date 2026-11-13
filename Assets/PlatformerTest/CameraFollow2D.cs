using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.2f, -10f);
    public float smooth = 8f;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
                target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 goal = target.position + offset;
        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, goal, t);
    }
}
