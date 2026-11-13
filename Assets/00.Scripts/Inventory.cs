using UnityEngine;

public class Inventory : MonoBehaviour
{
    public Transform parent;
    public GameObject slotPrefab;
    public SlotUI[,] grid = new SlotUI[10,5];

    public string itemName;
    public Vector2Int[] shape;
    public Sprite icon;

    public bool TryCheckSlot()
    {
        return false;
    }

    private void Awake()
    {
        int cnt = 0;
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for(int j = 0; j < grid.GetLength(1); j++)
            {
                GameObject go = Instantiate(slotPrefab, parent);
                go.name = cnt.ToString();
                grid[i, j] = go.GetComponent<SlotUI>();
                cnt++;
            }
        }
    }

    [ContextMenu("아이템 생성")]
    void CreateItem()
    {
        ItemData data = new ItemData()
        {
            itemName = this.itemName,
            shape = this.shape,
            icon = this.icon
        };
        Add(data);
    }


    //효율적인진 나중에 생각 ㄱㄱ
    public void Add(ItemData data)
    {
        foreach(SlotUI slot in grid)
        {
            if(slot.ItemData == null)
            {
                slot.ItemData = data;
                Debug.Log(slot.name);
                break;
            }
        }
    }

}
