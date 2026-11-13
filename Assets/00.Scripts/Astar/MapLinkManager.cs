using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

public class MapLinkManager : MonoBehaviour
{
    private static readonly List<NodeLink2> links = new();

    public static NodeLink2 Connect(
        Transform doorA,
        Transform doorB,
        float costFactor = 1f,
        bool oneWay = false)
    {
        if (doorA == null || doorB == null)
            return null;

        // 링크 오브젝트 생성
        GameObject obj = new GameObject("RoomLink");

        obj.transform.position = doorA.position;

        // NodeLink2 설정
        NodeLink2 link = obj.AddComponent<NodeLink2>();

        link.end = doorB;
        link.costFactor = costFactor;
        link.oneWay = oneWay;

        links.Add(link);

        // 그래프 변경은 안전한 작업 시점에 실행
        AstarPath.active.AddWorkItem(() =>
        {
            if (link != null)
                link.InternalOnPostScan();
        });

        return link;
    }

    public void Disconnect(NodeLink2 link)
    {
        if (link == null)
            return;

        links.Remove(link);
        Destroy(link.gameObject);
    }

    public void DisconnectAll()
    {
        foreach (var link in links)
        {
            if (link != null)
                Destroy(link.gameObject);
        }

        links.Clear();
    }
}
