using UnityEngine;

public class HotSwapTemp : MonoBehaviour
{

    public float speed = 10f;
    private void Update()
    {
        transform.Rotate(new Vector3(0, 0, speed * Time.deltaTime));
    }
}
