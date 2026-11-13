using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomEditor(typeof(MonoBehaviour), true)]
public class ButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MonoBehaviour mono = (MonoBehaviour)target;

        MethodInfo[] methods = mono.GetType().GetMethods(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (MethodInfo method in methods)
        {
            ButtonAttribute attribute =
                method.GetCustomAttribute<ButtonAttribute>();

            if (attribute == null)
                continue;

            // 파라미터가 없는 메서드만 지원
            if (method.GetParameters().Length > 0)
                continue;

            if (GUILayout.Button(method.Name))
            {
                method.Invoke(mono, null);
            }
        }
    }
}