using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITest : MonoBehaviour, IPointerClickHandler, 
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, 
    IPointerDownHandler, IPointerUpHandler
{
    public Button a;
    public bool isDrag;
    Vector3 startPos;
    Vector3 lastPos;

    RectTransform rt;
    Image image;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    void Update()
    {
        if (isDrag)
        {
            
            Vector2 mousePos = Input.mousePosition;
            rt.position = mousePos;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
     
    }



    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("엔터");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("익사이트");
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        Debug.Log("무으브");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDrag = true;
        startPos = rt.anchoredPosition;
        image.raycastTarget = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrag = false;

        //rt.position = MouseManager.instance.slot.transform.position;
        //MouseManager.instance.slot = null;
        image.raycastTarget = true;
    }

    
}
