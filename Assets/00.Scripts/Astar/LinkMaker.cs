using UnityEngine;

public class LinkMaker : MonoBehaviour
{
    public Transform doorA;
    public Transform doorB;

    [Button]
    public void StartLink()
    {
        MapLinkManager.Connect(doorA, doorB);
    }

    
}
