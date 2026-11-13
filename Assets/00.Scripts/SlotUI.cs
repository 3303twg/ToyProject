using UnityEngine;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour
{

    public ItemData itemData = null;
    public ItemData ItemData
    {
        get { return itemData; }
        set 
        {
            Debug.Log("?");
            itemData = value;
            RefrashSlot();
        }
    }
    public Image slotBg;
    public Image icon;


    private void Awake()
    {
        slotBg = GetComponent<Image>();
    }

    public void RefrashSlot()
    {
        icon.sprite = itemData.icon;
    }
}
