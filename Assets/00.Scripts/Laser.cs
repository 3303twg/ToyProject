
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Laser : MonoBehaviour
{
    public LineRenderer line;

    public LayerMask mask;
    public int maxBounce = 10;
    public float maxDistance = 100f;


    public float distance = 0;
    public float duration = 5f;
    private float timer = 0f;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.widthMultiplier = 0.15f;
        line.widthCurve = AnimationCurve.Constant(0, 1, 1);
        line.numCornerVertices = 8;
        line.numCapVertices = 8;
    }
    private void Update()
    {
        ShootLaser();
        Lerp();
    }

    void Lerp()
    {
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / duration);

        distance = Mathf.Lerp(0f, maxDistance, t);
    }
    void ShootLaser()
    {
        Vector2 origin = transform.position;
        Vector2 dir = transform.right;

        List<Vector3> points = new();
        points.Add(origin);

        for (int i = 0; i < maxBounce; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                origin, dir, distance, mask
            );

            if (hit.collider != null)
            {
                points.Add(hit.point);

                dir = Vector2.Reflect(dir, hit.normal);

                origin = hit.point + dir * 0.00001f;
            }
            else
            {
                points.Add(origin + dir * distance);
                break;
            }
        }

        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
    }
    
}
