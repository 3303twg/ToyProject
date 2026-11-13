using UnityEngine;

public class MouseManager : MonoBehaviour
{
    public static MouseManager instance;



    private void Awake()
    {
        instance = this;
    }
}
