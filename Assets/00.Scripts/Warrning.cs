using System;
using UnityEngine;

public class Warrning : MonoBehaviour
{
    public SpriteRenderer sr;


    public float maxVlue = 1f;
    public float value = 0;
    public float duration = 5f;
    private float timer = 0f;

    public event Action action;

    bool isAttacked = false;
    // Update is called once per frame
    void Update()
    {
        if (isAttacked)
            return;
        Lerp();
        

    }

    void Lerp()
    {
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / duration);

        value = Mathf.Lerp(0f, maxVlue, t);

        //sr.color.a = value;

        Color c = new Color(sr.color.r, sr.color.g, sr.color.b, value);
        sr.color = c;
        if (value >= maxVlue)
        {
            action.Invoke();
            isAttacked = true;
        }
    }
}
