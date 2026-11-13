using UnityEngine;

public class HotSwap : MonoBehaviour
{

    public float speed = 10f;
    public float moveSpeed = 30f;
    private void Update()
    {
        transform.Rotate(new Vector3(0, 0, -speed * Time.deltaTime));
        transform.Translate(transform.up * moveSpeed * Time.deltaTime);
    }
}
