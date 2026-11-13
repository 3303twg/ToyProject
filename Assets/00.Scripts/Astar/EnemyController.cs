using Pathfinding;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public AIDestinationSetter setter;

    private void Awake()
    {
        setter = GetComponent<AIDestinationSetter>();
    }

    private void Start()
    {
        setter.target = Player.instance.transform;
    }
}
