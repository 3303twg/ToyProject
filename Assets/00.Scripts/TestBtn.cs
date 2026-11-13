using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TestBtn : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    Button bt;
    public bool isPush = false;
    public Sprite normal;
    public Sprite push;
    Image sr;

    private void Awake()
    {
        sr = GetComponent<Image>();
        InvokeRepeating("OnClick", 0, 0.5f);
        Application.runInBackground = true;
    }

    public void OnClick()
    {
        isPush = !isPush;

        if (isPush)
            sr.sprite = push;
        else
            sr.sprite = normal;

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPush = true;
        sr.sprite = push;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPush = false;
        sr.sprite = normal;
    }
}
