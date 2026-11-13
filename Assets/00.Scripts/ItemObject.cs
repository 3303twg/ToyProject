using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemObject : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public ItemData data;
    
    public bool isDrag = false;
    Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }
    //구조 별론데
    private void Update()
    {
        if (isDrag)
        {
            transform.position = Input.mousePosition;
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        isDrag = true;
        image.raycastTarget = false;

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrag = false;
        image.raycastTarget = true;
    }


}
