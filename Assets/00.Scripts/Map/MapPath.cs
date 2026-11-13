
using UnityEngine;
using Pathfinding;

public class MapPath : MonoBehaviour
{
    public Vector2 mapSize;
    public float nodeSize = 0.5f;

    private GridGraph graph;
    public bool isView = false;

    BoxCollider2D cd;

    private void Awake()
    {
        cd = GetComponent<BoxCollider2D>();

        //Debug.Log(Vector2.Scale(cd.size, cd.transform.lossyScale));
        //mapSize = Vector2.Scale(cd.bounds.size, cd.transform.lossyScale);
        mapSize = cd.bounds.size;
        BuildGraph();
    }

    private void OnDrawGizmos()
    {
        if(isView)
            Gizmos.DrawCube(transform.position, mapSize);
    }


    [Button]
    public void BuildGraph()
    {
        var astar = AstarPath.active;

        // 현재 맵 전용 Grid Graph 생성
        //graph = astar.data.AddGraph<GridGraph>();
        graph = astar.data.AddGraph(typeof(GridGraph)) as GridGraph;

        graph.SetDimensions(
            Mathf.RoundToInt(mapSize.x / nodeSize),
            Mathf.RoundToInt(mapSize.y / nodeSize),
            nodeSize
        );
        graph.center = cd.bounds.center;
        graph.is2D = true;
        graph.collision.use2D= true;
        graph.collision.mask = LayerMask.GetMask("Wall");
        
        //비동기는 무료에서 안대는거로
        astar.Scan(graph);
    }

    [Button]
    public void RemoveGraph()
    {
        if (graph == null) return;

        AstarPath.active.data.RemoveGraph(graph);
        graph = null;
    }
}
