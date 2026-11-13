using UnityEngine;

public class Test : MonoBehaviour
{
    SpriteRenderer sr;
    public GameObject warrning;
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }
    public void Log()
    {
        //sr.flipX = !sr.flipX;
        //sr.flipY = !sr.flipY;

        Debug.Log("공격");
    }

    public void SpawnWarring()
    {
        GameObject go = Instantiate(warrning);
        Warrning wr = go.GetComponent<Warrning>();

        wr.action += Log;
    }
}
